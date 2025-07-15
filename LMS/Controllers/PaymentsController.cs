using LMS.Application.DTOs;
using LMS.Application.Services;
using LMS.Core.Interfaces.Services;
using LMS.Core.Models.PayMob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace LMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ICourseService _courseService;
        private readonly IPayMobService _payMobService;
        private readonly ILogger<PaymentsController> _logger;
        private readonly IUserService _userService;

        public PaymentsController(
            IPaymentService paymentService,
            ICourseService courseService,
            IPayMobService payMobService, IUserService userService,
            ILogger<PaymentsController> logger)

        {
            _paymentService = paymentService;
            _courseService = courseService;
            _payMobService = payMobService;
            _logger = logger;
            _userService = userService;
        }


        [HttpPost]
        public async Task<IActionResult> InitiatePayment(int courseId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdString);

            try
            {
                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                   
                    return RedirectToAction("Index", "Courses");
                }

                if (course.Price == null || course.Price <= 0)
                {
                    
                    return RedirectToAction("Details", "Courses", new { id = courseId });
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("Initiating PayMob payment for user {UserId}, course {CourseId}", userId, courseId);

                // Use real PayMob service to start payment
                var paymentUrl = await _payMobService.StartPaymentAsync(user, course);

                _logger.LogInformation("PayMob payment URL generated successfully, redirecting user");
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PayMob payment for course {CourseId}", courseId);
               
                return RedirectToAction("PayForCourse", new { courseId });
            }
        }

        [HttpGet("my-payments/{userId}")]
        // إضافة userId كـ parameter بدلاً من أخذه من Token
        public async Task<IActionResult> GetMyPayments(int userId)
        {
            try
            {
                var payments = await _paymentService.GetUserPaymentsAsync(userId);

                var paymentDtos = payments.Select(p => new PaymentDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    CourseId = p.CourseId,
                    CourseName = p.Course?.Title ?? "Unknown Course",
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Status = p.Status.ToString(),
                    Method = p.Method.ToString(),
                    CreatedDate = p.CreatedDate,
                    CompletedDate = p.CompletedDate,
                    FailureReason = p.FailureReason
                }).OrderByDescending(p => p.CreatedDate);

                return Ok(paymentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user payments for user {UserId}", userId);
                return StatusCode(500, new { Message = "An error occurred while retrieving payments" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);

                if (payment == null)
                    return NotFound("Payment not found");

                var paymentDto = new PaymentDto
                {
                    Id = payment.Id,
                    UserId = payment.UserId,
                    CourseId = payment.CourseId,
                    CourseName = payment.Course?.Title ?? "Unknown Course",
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status.ToString(),
                    Method = payment.Method.ToString(),
                    CreatedDate = payment.CreatedDate,
                    CompletedDate = payment.CompletedDate,
                    FailureReason = payment.FailureReason
                };

                return Ok(paymentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment {PaymentId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving payment" });
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                var payload = await new StreamReader(Request.Body).ReadToEndAsync();
                var signature = Request.Headers["X-PayMob-Signature"].FirstOrDefault();

                _logger.LogInformation("Received PayMob webhook");

                // Verify webhook signature (if configured)
                var isValid = await _payMobService.VerifyWebhookAsync(payload, signature);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return Unauthorized("Invalid signature");
                }

                // Process webhook data
                var transaction = await _payMobService.ProcessWebhookAsync(payload);
                if (transaction == null)
                {
                    _logger.LogError("Failed to process webhook data");
                    return BadRequest("Invalid webhook data");
                }

                // Update payment status based on transaction
                var paymentStatus = transaction.success ?
                    Core.Enums.PaymentStatus.Completed :
                    Core.Enums.PaymentStatus.Failed;

                var processed = await _paymentService.ProcessWebhookPaymentAsync(
                    transaction.id.ToString(),
                    paymentStatus);

                if (processed)
                {
                    _logger.LogInformation("Successfully processed webhook for transaction {TransactionId}",
                        transaction.id);
                    return Ok(new { Message = "Webhook processed successfully" });
                }
                else
                {
                    _logger.LogWarning("Failed to process webhook for transaction {TransactionId}",
                        transaction.id);
                    return BadRequest("Failed to process webhook");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayMob webhook");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("success")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string? payment_id)
        {
            try
            {
                if (string.IsNullOrEmpty(payment_id))
                    return BadRequest("Payment ID is required");

                return Redirect($"/payment/success?payment_id={payment_id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment success callback");
                return Redirect("/payment/error");
            }
        }

        [HttpGet("cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCancel([FromQuery] string? payment_id)
        {
            try
            {
                if (!string.IsNullOrEmpty(payment_id) && int.TryParse(payment_id, out int paymentIdInt))
                {
                    await _paymentService.FailPaymentAsync(paymentIdInt, "Payment cancelled by user");
                }

                return Redirect($"/payment/cancel?payment_id={payment_id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment cancel callback");
                return Redirect("/payment/error");
            }
        }
    }
}