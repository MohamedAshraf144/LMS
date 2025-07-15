using LMS.Core.Models;
using LMS.Core.Models.PayMob;

namespace LMS.Core.Interfaces.Services
{
    public interface IPayMobService
    {
        Task<string> GetAuthTokenAsync();
        Task<int> CreateOrderAsync(string authToken, decimal amount, string description, string merchantOrderId);
        Task<string> GetPaymentTokenAsync(string authToken, int orderId, decimal amount, User user);
        Task<string> GetPaymentUrlAsync(string paymentToken);
        Task<bool> VerifyWebhookAsync(string payload, string signature);
        Task<PaymentTransaction?> ProcessWebhookAsync(string payload);
        Task<string> StartPaymentAsync(User user, Course course);
    }
}
