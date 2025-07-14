// LMS.Application/Services/PayMobService.cs
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
        }

        public async Task<string> GetAuthTokenAsync()
        {
            try
            {
                var request = new AuthTokenRequest { api_key = _config.ApiKey };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/auth/tokens", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthTokenResponse>(responseJson);

                return authResponse?.token ?? throw new Exception("Failed to get auth token");
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

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/ecommerce/orders", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonSerializer.Deserialize<OrderResponse>(responseJson);

                return orderResponse?.id ?? throw new Exception("Failed to create order");
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

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/acceptance/payment_keys", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var paymentResponse = JsonSerializer.Deserialize<PaymentKeyResponse>(responseJson);

                return paymentResponse?.token ?? throw new Exception("Failed to get payment token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayMob payment token");
                throw;
            }
        }

        public Task<string> GetPaymentUrlAsync(string paymentToken)
        {
            var paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_config.IframeId}?payment_token={paymentToken}";
            return Task.FromResult(paymentUrl);
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

                return Task.FromResult(computedSignature == signature?.ToLower());
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
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var webhookData = JsonSerializer.Deserialize<WebhookData>(payload, options);
                return webhookData?.obj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayMob webhook");
                return null;
            }
        }
    }
}