using LMS.Core.Interfaces.Services;
using LMS.Core.Models;
using LMS.Core.Models.PayMob;
using Microsoft.Extensions.Logging;

namespace LMS.Application.Services
{
    public class MockPayMobService : IPayMobService
    {
        private readonly ILogger<MockPayMobService> _logger;

        // Remove HttpClient from constructor - it's not needed for mock service
        public MockPayMobService(ILogger<MockPayMobService> logger)
        {
            _logger = logger;
        }

        public Task<string> GetAuthTokenAsync()
        {
            _logger.LogInformation("Mock PayMob: Generating fake auth token");
            return Task.FromResult("mock_auth_token_12345");
        }

        public Task<int> CreateOrderAsync(string authToken, decimal amount, string description, string merchantOrderId)
        {
            _logger.LogInformation("Mock PayMob: Creating fake order for amount {Amount}", amount);
            var fakeOrderId = Random.Shared.Next(10000, 99999);
            return Task.FromResult(fakeOrderId);
        }

        public Task<string> GetPaymentTokenAsync(string authToken, int orderId, decimal amount, User user)
        {
            _logger.LogInformation("Mock PayMob: Generating fake payment token for order {OrderId}", orderId);
            var fakeToken = $"mock_payment_token_{orderId}_{Guid.NewGuid():N}";
            return Task.FromResult(fakeToken);
        }

        public Task<string> GetPaymentUrlAsync(string paymentToken)
        {
            _logger.LogInformation("Mock PayMob: Generating fake payment URL for token {Token}", paymentToken);

            // Create a mock payment URL that redirects to your MockPayment page
            var mockPaymentUrl = $"https://localhost:7095/Payment/MockPayment?token={paymentToken}";
            return Task.FromResult(mockPaymentUrl);
        }

        public Task<bool> VerifyWebhookAsync(string payload, string signature)
        {
            _logger.LogInformation("Mock PayMob: Verifying webhook (always returns true)");
            return Task.FromResult(true);
        }

        public Task<PaymentTransaction?> ProcessWebhookAsync(string payload)
        {
            _logger.LogInformation("Mock PayMob: Processing webhook");

            var mockTransaction = new PaymentTransaction
            {
                id = Random.Shared.Next(10000, 99999),
                success = true,
                amount_cents = 5000, // $50
                currency = "EGP",
                order = new TransactionOrder
                {
                    id = Random.Shared.Next(10000, 99999),
                    merchant_order_id = "mock_order"
                }
            };

            return Task.FromResult<PaymentTransaction?>(mockTransaction);
        }
    }
}