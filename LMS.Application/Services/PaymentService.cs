using LMS.Core.Interfaces;
using LMS.Core.Interfaces.Services;
using LMS.Core.Models;
using LMS.Core.Enums;
using Microsoft.Extensions.Logging;

namespace LMS.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IPayMobService _payMobService;
        private readonly IUserService _userService;
        private readonly ICourseService _courseService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IRepository<Payment> paymentRepository,
            IPayMobService payMobService,
            IUserService userService,
            ICourseService courseService,
            IEnrollmentService enrollmentService,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _payMobService = payMobService;
            _userService = userService;
            _courseService = courseService;
            _enrollmentService = enrollmentService;
            _logger = logger;
        }

        public async Task<Payment> CreatePaymentAsync(int userId, int courseId, decimal amount)
        {
            try
            {
                var payment = new Payment
                {
                    UserId = userId,
                    CourseId = courseId,
                    Amount = amount,
                    Currency = "EGP",
                    Status = PaymentStatus.Pending,
                    CreatedDate = DateTime.UtcNow
                };

                var createdPayment = await _paymentRepository.AddAsync(payment);
                _logger.LogInformation("Payment created with ID: {PaymentId}", createdPayment.Id);

                return createdPayment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for user {UserId}, course {CourseId}", userId, courseId);
                throw;
            }
        }


        public async Task<string> InitiatePaymentAsync(int paymentId)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    throw new InvalidOperationException("Payment not found");

                var user = await _userService.GetUserByIdAsync(payment.UserId);
                if (user == null)
                    throw new InvalidOperationException("User not found");

                var course = await _courseService.GetCourseByIdAsync(payment.CourseId);
                if (course == null)
                    throw new InvalidOperationException("Course not found");

                _logger.LogInformation("Initiating PayMob payment for PaymentId: {PaymentId}, Amount: {Amount}",
                    paymentId, payment.Amount);

                // Get PayMob auth token
                var authToken = await _payMobService.GetAuthTokenAsync();

                // Create order (استخدام payment.Amount مباشرة لأنه متحفوظ بـ EGP)
                var orderId = await _payMobService.CreateOrderAsync(
                    authToken,
                    payment.Amount / 30, // تحويل من EGP للـ PayMob Service
                    $"Course: {course.Title}",
                    payment.Id.ToString()
                );

                // Get payment token
                var paymentToken = await _payMobService.GetPaymentTokenAsync(
                    authToken,
                    orderId,
                    payment.Amount / 30, // تحويل من EGP
                    user
                );

                // Update payment record مع PayMob details (استخدام الـ fields الموجودة)
                payment.PayMobOrderId = orderId.ToString();
                payment.PaymentToken = paymentToken;
                payment.Status = PaymentStatus.Processing;
                await _paymentRepository.UpdateAsync(payment);

                // Get payment URL
                var paymentUrl = await _payMobService.GetPaymentUrlAsync(paymentToken);

                _logger.LogInformation("Payment initiated successfully: PaymentId={PaymentId}, OrderId={OrderId}",
                    paymentId, orderId);

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<Payment?> GetPaymentByIdAsync(int id)
        {
            return await _paymentRepository.GetByIdAsync(id);
        }

        public async Task<Payment?> GetPaymentByOrderIdAsync(string orderId)
        {
            var payments = await _paymentRepository.FindAsync(p => p.PayMobOrderId == orderId);
            return payments.FirstOrDefault();
        }

        public async Task<bool> CompletePaymentAsync(int paymentId, string transactionId)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    return false;

                payment.Status = PaymentStatus.Completed;
                payment.PayMobTransactionId = transactionId;
                payment.CompletedDate = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                // Auto-enroll user in course
                try
                {
                    await _enrollmentService.EnrollUserInCourseAsync(payment.UserId, payment.CourseId);
                    _logger.LogInformation("User {UserId} auto-enrolled in course {CourseId} after payment {PaymentId}",
                        payment.UserId, payment.CourseId, paymentId);
                }
                catch (InvalidOperationException)
                {
                    // User might already be enrolled
                    _logger.LogWarning("User {UserId} already enrolled in course {CourseId}",
                        payment.UserId, payment.CourseId);
                }

                _logger.LogInformation("Payment {PaymentId} completed successfully", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<bool> FailPaymentAsync(int paymentId, string reason)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    return false;

                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = reason;
                await _paymentRepository.UpdateAsync(payment);

                _logger.LogInformation("Payment {PaymentId} marked as failed: {Reason}", paymentId, reason);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error failing payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId)
        {
            return await _paymentRepository.FindAsync(p => p.UserId == userId);
        }

        public async Task<bool> ProcessWebhookPaymentAsync(string payMobTransactionId, PaymentStatus status)
        {
            try
            {
                var payments = await _paymentRepository.FindAsync(p => p.PayMobTransactionId == payMobTransactionId);
                var payment = payments.FirstOrDefault();

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for transaction ID: {TransactionId}", payMobTransactionId);
                    return false;
                }

                if (status == PaymentStatus.Completed)
                {
                    return await CompletePaymentAsync(payment.Id, payMobTransactionId);
                }
                else if (status == PaymentStatus.Failed)
                {
                    return await FailPaymentAsync(payment.Id, "Payment failed via webhook");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook payment for transaction {TransactionId}", payMobTransactionId);
                return false;
            }
        }
    }
}