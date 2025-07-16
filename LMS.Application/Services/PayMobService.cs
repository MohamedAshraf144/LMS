// ================================================================================================
// Enhanced PayMobService.cs for Real Integration - FIXED VERSION
// File: LMS.Application/Services/PayMobService.cs
// ================================================================================================

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
            ConfigureHttpClient();
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

            _logger.LogInformation("PayMob configuration validated successfully");
            _logger.LogDebug("PayMob Config - IntegrationId: {IntegrationId}, IframeId: {IframeId}",
                _config.IntegrationId, _config.IframeId);
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LMS-PayMob-Integration/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> StartPaymentAsync(User user, Course course)
        {
            try
            {
                _logger.LogInformation("🚀 Starting PayMob payment flow for user {UserId} and course {CourseId}",
                    user.Id, course.Id);

                if (course.Price == null || course.Price <= 0)
                {
                    throw new InvalidOperationException("Course is free or has no price set");
                }

                // Generate unique merchant order ID
                var merchantOrderId = $"LMS_COURSE_{course.Id}_USER_{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}";

                // Convert USD to EGP (assuming course.Price is in USD)
                var priceUSD = course.Price.Value;
                var priceEGP = priceUSD * 30; // 1 USD = 30 EGP (update this rate as needed)
                var amountCents = (int)(priceEGP * 100); // Convert to cents

                _logger.LogInformation("💰 Payment details: ${PriceUSD} USD = {PriceEGP} EGP = {AmountCents} cents",
                    priceUSD, priceEGP, amountCents);

                // Step 1: Get Authentication Token
                _logger.LogInformation("🔐 Step 1: Getting authentication token...");
                var authToken = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(authToken))
                {
                    throw new InvalidOperationException("Failed to get PayMob authentication token");
                }
                _logger.LogInformation("✅ Auth token received: {TokenLength} characters", authToken.Length);

                // Step 2: Create Order
                _logger.LogInformation("📦 Step 2: Creating PayMob order...");
                var orderId = await CreateOrderAsync(authToken, course.Price.Value, $"Course: {course.Title}", merchantOrderId);
                if (orderId <= 0)
                {
                    throw new InvalidOperationException("Failed to create PayMob order");
                }
                _logger.LogInformation("✅ Order created with ID: {OrderId}", orderId);

                // Step 3: Get Payment Key
                _logger.LogInformation("🔑 Step 3: Getting payment key...");
                var paymentKey = await GetPaymentTokenAsync(authToken, orderId, course.Price.Value, user);
                if (string.IsNullOrEmpty(paymentKey))
                {
                    throw new InvalidOperationException("Failed to get PayMob payment key");
                }
                _logger.LogInformation("✅ Payment key received: {KeyLength} characters", paymentKey.Length);

                // Step 4: Generate iframe URL
                var iframeUrl = await GetPaymentUrlAsync(paymentKey);
                _logger.LogInformation("🌐 Payment URL generated: {PaymentUrl}", iframeUrl);

                return iframeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in StartPaymentAsync for user {UserId} and course {CourseId}",
                    user.Id, course.Id);
                throw;
            }
        }

        public async Task<string> GetAuthTokenAsync()
        {
            try
            {
                _logger.LogInformation("Requesting PayMob authentication token");

                var request = new { api_key = _config.ApiKey };
                var response = await PostAsync<AuthTokenResponse>("/auth/tokens", request);

                if (string.IsNullOrEmpty(response?.token))
                {
                    _logger.LogError("PayMob auth response missing token");
                    throw new InvalidOperationException("Failed to get authentication token from PayMob");
                }

                _logger.LogInformation("PayMob authentication token received successfully");
                return response.token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayMob authentication token");
                throw;
            }
        }

        public async Task<int> CreateOrderAsync(string authToken, decimal amount, string description, string merchantOrderId)
        {
            try
            {
                // Convert from USD to EGP (1 USD = 30 EGP approximately)
                var amountEGP = amount * 30;
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

                var response = await PostAsync<OrderResponse>("/ecommerce/orders", request);

                if (response?.id == 0 || response?.id == null)
                {
                    _logger.LogError("PayMob order response missing ID");
                    throw new InvalidOperationException("Failed to create order - missing order ID");
                }

                _logger.LogInformation("PayMob order created successfully with ID {OrderId}", response.id);
                return response.id;
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
                var amountEGP = amount * 30;
                var amountCents = (int)(amountEGP * 100);

                _logger.LogInformation("Getting payment token for order {OrderId}, user {UserEmail}", orderId, user.Email);

                var request = new
                {
                    auth_token = authToken,
                    amount_cents = amountCents,
                    expiration = 3600, // 1 hour
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
                    integration_id = int.Parse(_config.IntegrationId),
                    lock_order_when_paid = true
                };

                var response = await PostAsync<PaymentKeyResponse>("/acceptance/payment_keys", request);

                if (string.IsNullOrEmpty(response?.token))
                {
                    _logger.LogError("PayMob payment key response missing token");
                    throw new InvalidOperationException("Failed to get payment token");
                }

                _logger.LogInformation("PayMob payment token received successfully");
                return response.token;
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
                _logger.LogInformation("Generated PayMob payment URL successfully");
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
                    _logger.LogWarning("Webhook secret not configured, accepting all webhooks (NOT RECOMMENDED FOR PRODUCTION)");
                    return Task.FromResult(true);
                }

                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("No webhook signature provided");
                    return Task.FromResult(false);
                }

                // Calculate HMAC signature
                using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_config.WebhookSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var calculatedSignature = Convert.ToHexString(hash).ToLower();

                var isValid = string.Equals(calculatedSignature, signature.ToLower(), StringComparison.OrdinalIgnoreCase);

                _logger.LogDebug("Webhook signature verification: {IsValid}", isValid);

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
                _logger.LogDebug("Webhook payload: {Payload}", payload);

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

                _logger.LogInformation("Processed webhook for transaction {TransactionId} - Success: {Success}, Amount: {Amount}",
                    webhookData.obj.id, webhookData.obj.success, webhookData.obj.amount_cents);

                return webhookData.obj;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing PayMob webhook JSON");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayMob webhook");
                return null;
            }
        }

        // Helper method for making HTTP requests
        private async Task<T?> PostAsync<T>(string endpoint, object data) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("PayMob API Request to {Endpoint}: {Request}", endpoint, json);

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("PayMob API Response from {Endpoint}: {StatusCode} - {Response}",
                    endpoint, response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMob API request to {Endpoint} failed: {StatusCode} - {Response}",
                        endpoint, response.StatusCode, responseContent);
                    throw new HttpRequestException($"PayMob API request failed: {response.StatusCode}");
                }

                if (string.IsNullOrEmpty(responseContent))
                {
                    _logger.LogWarning("Empty response from PayMob API endpoint {Endpoint}", endpoint);
                    return null;
                }

                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making request to PayMob API endpoint {Endpoint}", endpoint);
                throw;
            }
        }

        // Method to get payment status (useful for checking payment manually)
        public async Task<PaymentTransaction?> GetPaymentStatusAsync(string authToken, int transactionId)
        {
            try
            {
                _logger.LogInformation("Getting payment status for transaction {TransactionId}", transactionId);

                var response = await _httpClient.GetAsync($"/acceptance/transactions/{transactionId}?auth_token={authToken}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get payment status: {StatusCode} - {Response}",
                        response.StatusCode, responseContent);
                    return null;
                }

                var transaction = JsonSerializer.Deserialize<PaymentTransaction>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Payment status retrieved: TransactionId={TransactionId}, Success={Success}",
                    transactionId, transaction?.success);

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for transaction {TransactionId}", transactionId);
                return null;
            }
        }

        // Method to refund payment (if needed)
        public async Task<bool> RefundPaymentAsync(string authToken, int transactionId, decimal amount)
        {
            try
            {
                _logger.LogInformation("Initiating refund for transaction {TransactionId}, amount {Amount}",
                    transactionId, amount);

                var amountCents = (int)(amount * 100);
                var request = new
                {
                    auth_token = authToken,
                    transaction_id = transactionId,
                    amount_cents = amountCents
                };

                var response = await PostAsync<object>("/ecommerce/orders/refund", request);

                _logger.LogInformation("Refund initiated successfully for transaction {TransactionId}", transactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating refund for transaction {TransactionId}", transactionId);
                return false;
            }
        }
    }
}

// ================================================================================================
// PayMob Exception Class for Better Error Handling
// ================================================================================================

namespace LMS.Application.Services
{
    public class PayMobException : Exception
    {
        public string? ErrorCode { get; }
        public string? PayMobMessage { get; }

        public PayMobException(string message) : base(message) { }

        public PayMobException(string message, Exception innerException) : base(message, innerException) { }

        public PayMobException(string message, string errorCode, string payMobMessage) : base(message)
        {
            ErrorCode = errorCode;
            PayMobMessage = payMobMessage;
        }
    }
}