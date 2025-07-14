using LMS.Core.Enums;
using LMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Interfaces.Services
{

    public interface IPaymentService
    {
        Task<Payment> CreatePaymentAsync(int userId, int courseId, decimal amount);
        Task<string> InitiatePaymentAsync(int paymentId);
        Task<Payment?> GetPaymentByIdAsync(int id);
        Task<Payment?> GetPaymentByOrderIdAsync(string orderId);
        Task<bool> CompletePaymentAsync(int paymentId, string transactionId);
        Task<bool> FailPaymentAsync(int paymentId, string reason);
        Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId);
        Task<bool> ProcessWebhookPaymentAsync(string payMobTransactionId, PaymentStatus status);
    }
}
