// LMS.Web/Controllers/PaymentController.cs - محدث للتعامل مع API
using LMS.Application.DTOs;
using LMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LMS.Web.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ICourseService _courseService;
        private readonly ILogger<PaymentController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PaymentController(
            IPaymentService paymentService,
            ICourseService courseService,
            ILogger<PaymentController> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _paymentService = paymentService;
            _courseService = courseService;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    TempData["Warning"] = "Please login to view your payments";
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdString);
                var payments = await _paymentService.GetUserPaymentsAsync(userId);
                return View(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payments page");
                TempData["Error"] = "An error occurred while loading payments";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> PayForCourse(int courseId)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    TempData["Warning"] = "Please login to make a payment";
                    return RedirectToAction("Login", "Account");
                }

                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    TempData["Error"] = "Course not found";
                    return RedirectToAction("Index", "Courses");
                }

                if (course.Price == null || course.Price <= 0)
                {
                    TempData["Error"] = "This course is free";
                    return RedirectToAction("Details", "Courses", new { id = courseId });
                }

                ViewBag.Course = course;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment page for course {CourseId}", courseId);
                TempData["Error"] = "An error occurred while loading the payment page";
                return RedirectToAction("Index", "Courses");
            }
        }

        [HttpPost]
        public async Task<IActionResult> InitiatePayment(int courseId)
        {
            try
            {
                _logger.LogInformation("Initiating payment for course {CourseId}", courseId);

                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogWarning("User not logged in when trying to initiate payment");
                    TempData["Warning"] = "Please login to make a payment";
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdString);
                _logger.LogInformation("User {UserId} initiating payment for course {CourseId}", userId, courseId);

                // استخدام API بدلاً من الخدمة المباشرة
                var apiBaseUrl = _configuration["ApplicationSettings:ApiBaseUrl"] ?? "https://localhost:7278/api";
                var apiUrl = $"{apiBaseUrl}/Payments/initiate";

                var requestData = new
                {
                    courseId = courseId,
                    userId = userId
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling API: {ApiUrl}", apiUrl);

                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call failed with status {StatusCode}: {Content}",
                        response.StatusCode, responseContent);

                    TempData["Error"] = $"Payment service error: {response.StatusCode}";
                    return RedirectToAction("PayForCourse", new { courseId });
                }

                var paymentResponse = JsonSerializer.Deserialize<PaymentInitiationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (paymentResponse?.PaymentUrl == null)
                {
                    _logger.LogError("API returned null payment URL");
                    TempData["Error"] = "Payment service returned invalid response";
                    return RedirectToAction("PayForCourse", new { courseId });
                }

                _logger.LogInformation("Payment URL received: {PaymentUrl}", paymentResponse.PaymentUrl);

                // إعادة توجيه إلى رابط الدفع
                return Redirect(paymentResponse.PaymentUrl);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Network error while calling payment API for course {CourseId}", courseId);
                TempData["Error"] = "Network error. Please check your connection and try again.";
                return RedirectToAction("PayForCourse", new { courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error initiating payment for course {CourseId}: {Message}", courseId, ex.Message);
                TempData["Error"] = $"An error occurred while initiating payment: {ex.Message}";
                return RedirectToAction("PayForCourse", new { courseId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(string? payment_id)
        {
            try
            {
                ViewBag.PaymentId = payment_id;
                ViewBag.Message = "Payment successful! You will be enrolled in the course shortly.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing payment success page");
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(string? payment_id)
        {
            try
            {
                ViewBag.PaymentId = payment_id;
                ViewBag.Message = "Payment was cancelled. You can try again.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing payment cancel page");
                return View("Error");
            }
        }

        [HttpGet]
        public IActionResult Error()
        {
            ViewBag.Message = "An error occurred during payment. Please try again or contact support.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    TempData["Warning"] = "Please login to view payment details";
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdString);
                var payment = await _paymentService.GetPaymentByIdAsync(id);

                if (payment == null)
                {
                    TempData["Error"] = "Payment not found";
                    return RedirectToAction("Index");
                }

                if (payment.UserId != userId)
                {
                    TempData["Error"] = "You don't have permission to view this payment";
                    return RedirectToAction("Index");
                }

                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing payment details {PaymentId}", id);
                TempData["Error"] = "An error occurred while loading payment details";
                return RedirectToAction("Index");
            }
        }

        // Test endpoint to verify payment flow
        [HttpGet]
        public async Task<IActionResult> TestPayment(int courseId)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    TempData["Warning"] = "Please login to test payment";
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdString);

                // إنشاء payment record مباشرة للاختبار
                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course?.Price != null)
                {
                    var payment = await _paymentService.CreatePaymentAsync(userId, courseId, course.Price.Value);

                    // تسجيل الدفع كمكتمل للاختبار
                    await _paymentService.CompletePaymentAsync(payment.Id, $"TEST_{payment.Id}");

                    TempData["Success"] = "Test payment completed successfully!";
                    return RedirectToAction("Success", new { payment_id = payment.Id });
                }

                TempData["Error"] = "Course not found or has no price";
                return RedirectToAction("PayForCourse", new { courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test payment for course {CourseId}", courseId);
                TempData["Error"] = $"Test payment failed: {ex.Message}";
                return RedirectToAction("PayForCourse", new { courseId });
            }

        }
        // إضافة route للـ Mock Payment في PaymentController
        // أضف هذا Method إلى LMS.Web/Controllers/PaymentController.cs

        [HttpGet]
        public IActionResult EnterTestCard(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterTestCard(int courseId, string cardNumber, string cardName, string expiryMonth, string expiryYear, string cvv)
        {
            // يمكنك إضافة تحقق من صحة البيانات هنا إذا أردت
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["Warning"] = "Please login to make a payment";
                return RedirectToAction("Login", "Account");
            }
            var userId = int.Parse(userIdString);
            var course = await _courseService.GetCourseByIdAsync(courseId);
            if (course?.Price != null)
            {
                var payment = await _paymentService.CreatePaymentAsync(userId, courseId, course.Price.Value);
                await _paymentService.CompletePaymentAsync(payment.Id, $"TESTCARD_{payment.Id}");
                TempData["Success"] = "Test card payment completed successfully!";
                // إعادة التوجيه مباشرة إلى صفحة تفاصيل الكورس
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }
            TempData["Error"] = "Course not found or has no price";
            return RedirectToAction("PayForCourse", new { courseId });
        }

        [HttpGet]
        public IActionResult MockPayment(string token)
        {
            ViewBag.Token = token;
            return View();
        }
    }
    // DTO للاستجابة من API
    public class PaymentInitiationResponse
    {
        public int PaymentId { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}