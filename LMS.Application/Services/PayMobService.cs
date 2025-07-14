// LMS.Application/Services/PayMobService.cs - Enhanced with better error handling
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

            // Validate configuration
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

            if (string.IsNullOrEmpty(_config.BaseUrl))
                errors.Add("PayMob BaseUrl is not configured");

            if (errors.Any())
            {
                var errorMessage = "PayMob configuration errors: " + string.Join(", ", errors);
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation("PayMob configuration validated successfully");
        }

        public async Task<string> GetAuthTokenAsync()
        {
            try
            {
                _logger.LogInformation("Requesting PayMob auth token");

                var request = new AuthTokenRequest { api_key = _config.ApiKey };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending auth request to: {Url}", $"{_config.BaseUrl}/auth/tokens");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/auth/tokens", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("PayMob auth response status: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMob auth failed with status {StatusCode}: {Content}",
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"PayMob auth failed: {response.StatusCode} - {responseContent}");
                }

                var authResponse = JsonSerializer.Deserialize<AuthTokenResponse>(responseContent);

                if (authResponse?.token == null)
                {
                    _logger.LogError("PayMob auth response missing token: {Content}", responseContent);
                    throw new InvalidOperationException("PayMob auth response missing token");
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
                _logger.LogInformation("Creating PayMob order for amount {Amount}", amount);

                var amountCents = (int)(amount * 100);
                var request = new OrderRequest
                {
                    auth_token = authToken,
                    amount_cents = amountCents,
                    items = new List<OrderItem>
                    {
                        new OrderItem
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

                _logger.LogDebug("Sending order request to: {Url}", $"{_config.BaseUrl}/ecommerce/orders");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/ecommerce/orders", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("PayMob order response status: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMob order creation failed with status {StatusCode}: {Content}",
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"PayMob order creation failed: {response.StatusCode} - {responseContent}");
                }

                var orderResponse = JsonSerializer.Deserialize<OrderResponse>(responseContent);

                if (orderResponse?.id == 0)
                {
                    _logger.LogError("PayMob order response missing ID: {Content}", responseContent);
                    throw new InvalidOperationException("PayMob order response missing ID");
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
                _logger.LogInformation("Getting PayMob payment token for order {OrderId}", orderId);

                var amountCents = (int)(amount * 100);
                var request = new PaymentKeyRequest
                {
                    auth_token = authToken,
                    amount_cents = amountCents,
                    order_id = orderId,
                    integration_id = _config.IntegrationId,
                    billing_data = new BillingData
                    {
                        email = user.Email,
                        first_name = user.FirstName,
                        last_name = user.LastName,
                        phone_number = user.Phone ?? "01000000000"
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending payment key request to: {Url}", $"{_config.BaseUrl}/acceptance/payment_keys");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/acceptance/payment_keys", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("PayMob payment key response status: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMob payment key creation failed with status {StatusCode}: {Content}",
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"PayMob payment key creation failed: {response.StatusCode} - {responseContent}");
                }

                var paymentResponse = JsonSerializer.Deserialize<PaymentKeyResponse>(responseContent);

                if (paymentResponse?.token == null)
                {
                    _logger.LogError("PayMob payment key response missing token: {Content}", responseContent);
                    throw new InvalidOperationException("PayMob payment key response missing token");
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
                    _logger.LogWarning("Webhook secret not configured, skipping signature verification");
                    return Task.FromResult(true);
                }

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.WebhookSecret));
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedSignature = Convert.ToHexString(computedHash).ToLower();

                var isValid = computedSignature == signature?.ToLower();
                _logger.LogDebug("Webhook signature verification result: {IsValid}", isValid);

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

                _logger.LogInformation("Processed webhook for transaction {TransactionId}", webhookData.obj.id);
                return webhookData.obj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayMob webhook");
                return null;
            }
        }
    }
}