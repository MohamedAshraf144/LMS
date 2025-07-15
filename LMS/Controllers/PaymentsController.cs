using LMS.Application.DTOs;
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

        public PaymentsController(
            IPaymentService paymentService,
            ICourseService courseService,
            IPayMobService payMobService,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _courseService = courseService;
            _payMobService = payMobService;
            _logger = logger;
        }


        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] CreatePaymentDtoWithUser dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = dto.UserId;
                _logger.LogInformation("Initiating payment for user {UserId}, course {CourseId}", userId, dto.CourseId);

                // Get course details
                var course = await _courseService.GetCourseByIdAsync(dto.CourseId);
                if (course == null)
                    return NotFound("Course not found");

                if (course.Price == null || course.Price <= 0)
                    return BadRequest("Course is free or price not set");

                // تحويل من USD إلى EGP قبل حفظ payment
                var priceEGP = course.Price.Value * 30; // تحديث السعر حسب السوق

                _logger.LogInformation("Course price: ${Price} USD = {PriceEGP} EGP", course.Price.Value, priceEGP);

                // Create payment record بالسعر المحول
                var payment = await _paymentService.CreatePaymentAsync(userId, dto.CourseId, priceEGP);

                // Initiate PayMob payment
                var paymentUrl = await _paymentService.InitiatePaymentAsync(payment.Id);

                var response = new PaymentInitiationResponse
                {
                    PaymentId = payment.Id,
                    PaymentUrl = paymentUrl,
                    Message = "Payment initiated successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment for course {CourseId}", dto.CourseId);
                return StatusCode(500, new
                {
                    Message = "An error occurred while initiating payment",
                    Error = ex.Message
                });
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