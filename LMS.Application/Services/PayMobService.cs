using LMS.Core.Interfaces.Services;
using LMS.Core.Models;
using LMS.Core.Models.PayMob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LMS.Application.Services
{
    public class PayMobService : IPayMobService
    {
        private readonly HttpClient _httpClient;
        private readonly PayMobConfiguration _config;
        private readonly ILogger<PayMobService> _logger;

        public PayMobService(HttpClient httpClient, IConfiguration configuration, ILogger<PayMobService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = new PayMobConfiguration();
            configuration.GetSection("PayMob").Bind(_config);

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(_config.ApiKey))
                errors.Add("PayMob ApiKey is not configured");
            if (string.IsNullOrEmpty(_config.IntegrationId))
                errors.Add("PayMob IntegrationId is not configured");
            if (string.IsNullOrEmpty(_config.IframeId))
                errors.Add("PayMob IframeId is not configured");

            if (errors.Any())
            {
                var errorMessage = "PayMob configuration errors: " + string.Join(", ", errors);
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation("PayMob configuration validated - IntegrationId: {IntegrationId}, IframeId: {IframeId}",
                _config.IntegrationId, _config.IframeId);
        }

        public async Task<string> GetAuthTokenAsync()
        {
            try
            {
                _logger.LogInformation("Requesting PayMob auth token");

                var request = new { api_key = _config.ApiKey };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/auth/tokens", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("PayMob auth response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMob auth failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"PayMob auth failed: {response.StatusCode}");
                }

                var authResponse = JsonSerializer.Deserialize<AuthTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (string.IsNullOrEmpty(authResponse?.token))
                {
                    _logger.LogError("PayMob auth response missing token");
                    throw new InvalidOperationException("Missing auth token");
                }

                _logger.LogInformation("PayMob auth token received successfully");
                return authResponse.token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayMob auth token");
                throw;
            }
        }

        public async Task<int> CreateOrderAsync(string authToken, decimal amount, string description, string merchantOrderId)
        {
            try
            {
                // تحويل من USD إلى EGP
                var amountEGP = amount * 30; // 1 USD = 30 EGP تقريباً
                var amountCents = (int)(amountEGP * 100);

                _logger.LogInformation("Creating PayMob order - Amount: {AmountUSD} USD = {AmountEGP} EGP ({AmountCents} cents)",
                    amount, amountEGP, amountCents);

                var request = new
                {
                    auth_token = authToken,
                    delivery_needed = false,
                    amount_cents = amountCents,
                    currency = "EGP",
                    merchant_order_id = merchantOrderId,
                    items = new[]
                    {
                        new
                        {
                            name = "Course Enrollment",
                            description = description,
                            amount_cents = amountCents,
                            quantity = 1
                        }
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("PayMob order request: {Payload}", json);

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/ecommerce/orders", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("PayMob order response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMob order creation failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"Order creation failed: {response.StatusCode}");
                }

                var orderResponse = JsonSerializer.Deserialize<OrderResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (orderResponse?.id == 0)
                {
                    _logger.LogError("PayMob order response missing ID");
                    throw new InvalidOperationException("Missing order ID");
                }

                _logger.LogInformation("PayMob order created successfully with ID {OrderId}", orderResponse.id);
                return orderResponse.id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayMob order");
                throw;
            }
        }

        public async Task<string> GetPaymentTokenAsync(string authToken, int orderId, decimal amount, User user)
        {
            try
            {
                var amountEGP = amount * 30; // تحويل لـ EGP
                var amountCents = (int)(amountEGP * 100);

                _logger.LogInformation("Getting payment token for order {OrderId}, user {UserEmail}", orderId, user.Email);

                var request = new
                {
                    auth_token = authToken,
                    amount_cents = amountCents,
                    expiration = 3600,
                    order_id = orderId,
                    billing_data = new
                    {
                        apartment = "NA",
                        email = user.Email,
                        floor = "NA",
                        first_name = user.FirstName,
                        street = "NA",
                        building = "NA",
                        phone_number = user.Phone ?? "01000000000",
                        shipping_method = "NA",
                        postal_code = "NA",
                        city = "Cairo",
                        country = "EG",
                        last_name = user.LastName,
                        state = "NA"
                    },
                    currency = "EGP",
                    integration_id = _config.IntegrationId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("PayMob payment key request: {Payload}", json);

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/acceptance/payment_keys", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("PayMob payment key response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMob payment key creation failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"Payment key creation failed: {response.StatusCode}");
                }

                var paymentResponse = JsonSerializer.Deserialize<PaymentKeyResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (string.IsNullOrEmpty(paymentResponse?.token))
                {
                    _logger.LogError("PayMob payment key response missing token");
                    throw new InvalidOperationException("Missing payment token");
                }

                _logger.LogInformation("PayMob payment token received successfully");
                return paymentResponse.token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayMob payment token");
                throw;
            }
        }

        public Task<string> GetPaymentUrlAsync(string paymentToken)
        {
            try
            {
                var paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_config.IframeId}?payment_token={paymentToken}";
                _logger.LogInformation("Generated PayMob payment URL: {PaymentUrl}", paymentUrl);
                return Task.FromResult(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PayMob payment URL");
                throw;
            }
        }

        public Task<bool> VerifyWebhookAsync(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(_config.WebhookSecret))
                {
                    _logger.LogWarning("Webhook secret not configured, accepting all webhooks");
                    return Task.FromResult(true);
                }

                // حساب HMAC signature
                using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_config.WebhookSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var calculatedSignature = Convert.ToHexString(hash).ToLower();

                var isValid = string.Equals(calculatedSignature, signature?.ToLower(), StringComparison.OrdinalIgnoreCase);

                _logger.LogDebug("Webhook signature verification: {IsValid} (Expected: {Expected}, Received: {Received})",
                    isValid, calculatedSignature, signature);

                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook signature");
                return Task.FromResult(false);
            }
        }

        public async Task<PaymentTransaction?> ProcessWebhookAsync(string payload)
        {
            try
            {
                _logger.LogInformation("Processing PayMob webhook");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var webhookData = JsonSerializer.Deserialize<WebhookData>(payload, options);

                if (webhookData?.obj == null)
                {
                    _logger.LogWarning("Webhook data missing transaction object");
                    return null;
                }

                _logger.LogInformation("Processed webhook for transaction {TransactionId} with success: {Success}",
                    webhookData.obj.id, webhookData.obj.success);

                return webhookData.obj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayMob webhook");
                return null;
            }
        }

        // ===============================
        // Implementation of StartPaymentAsync
        // ===============================
        public async Task<string> StartPaymentAsync(User user, Course course)
        {
            try
            {
                _logger.LogInformation("Starting payment for user {UserId} and course {CourseId}", user.Id, course.Id);

                if (course.Price == null || course.Price <= 0)
                {
                    throw new InvalidOperationException("Course is free or has no price set");
                }

                // Get auth token
                var authToken = await GetAuthTokenAsync();

                // Create order
                var merchantOrderId = $"COURSE_{course.Id}_USER_{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                var orderId = await CreateOrderAsync(
                    authToken,
                    course.Price.Value,
                    $"Course: {course.Title}",
                    merchantOrderId
                );

                // Get payment token
                var paymentToken = await GetPaymentTokenAsync(
                    authToken,
                    orderId,
                    course.Price.Value,
                    user
                );

                // Generate payment URL
                var paymentUrl = await GetPaymentUrlAsync(paymentToken);

                _logger.LogInformation("Payment started successfully for user {UserId}, course {CourseId}, order {OrderId}",
                    user.Id, course.Id, orderId);

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting payment for user {UserId} and course {CourseId}", user.Id, course.Id);
                throw;
            }
        }
    }
}
