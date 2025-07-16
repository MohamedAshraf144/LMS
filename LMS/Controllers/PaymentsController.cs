// Enhanced PaymentController.cs with Debug Logging
using LMS.Application.DTOs;
using LMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Web.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ICourseService _courseService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IUserService _userService;
        private readonly IPayMobService _payMobService;

        public PaymentController(
            IPaymentService paymentService,
            ICourseService courseService,
            ILogger<PaymentController> logger,
            IUserService userService,
            IPayMobService payMobService)
        {
            _paymentService = paymentService;
            _courseService = courseService;
            _logger = logger;
            _userService = userService;
            _payMobService = payMobService;
        }

        [HttpGet]
        public async Task<IActionResult> PayForCourse(int courseId)
        {
            try
            {
                _logger.LogInformation("🎯 PayForCourse GET - CourseId: {CourseId}", courseId);

                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogWarning("❌ User not logged in");
                    TempData["Warning"] = "Please login to make a payment";
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("✅ User logged in: {UserId}", userIdString);

                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    _logger.LogError("❌ Course not found: {CourseId}", courseId);
                    TempData["Error"] = "Course not found";
                    return RedirectToAction("Index", "Courses");
                }

                _logger.LogInformation("✅ Course found: {Title}, Price: {Price}", course.Title, course.Price);

                if (course.Price == null || course.Price <= 0)
                {
                    _logger.LogWarning("❌ Course is free: {Price}", course.Price);
                    TempData["Error"] = "This course is free";
                    return RedirectToAction("Details", "Courses", new { id = courseId });
                }

                ViewBag.Course = course;
                _logger.LogInformation("✅ PayForCourse view loaded successfully");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in PayForCourse GET");
                TempData["Error"] = "An error occurred while loading the payment page";
                return RedirectToAction("Index", "Courses");
            }
        }

        [HttpPost]
        public async Task<IActionResult> InitiatePayment(int courseId)
        {
            try
            {
                _logger.LogInformation("🚀 InitiatePayment POST started - CourseId: {CourseId}", courseId);

                // Check session
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogError("❌ No user session found");
                    TempData["Error"] = "Session expired. Please login again.";
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdString);
                _logger.LogInformation("✅ User session found: UserId = {UserId}", userId);

                // Validate course
                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    _logger.LogError("❌ Course not found in InitiatePayment: {CourseId}", courseId);
                    TempData["Error"] = "Course not found";
                    return RedirectToAction("PayForCourse", new { courseId });
                }

                _logger.LogInformation("✅ Course validated: {Title}, Price: ${Price}", course.Title, course.Price);

                if (course.Price == null || course.Price <= 0)
                {
                    _logger.LogError("❌ Course is free or has no price");
                    TempData["Error"] = "This course is free";
                    return RedirectToAction("Details", "Courses", new { id = courseId });
                }

                // Validate user
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("❌ User not found: {UserId}", userId);
                    TempData["Error"] = "User account not found";
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("✅ User validated: {Email}", user.Email);

                // Test PayMob connection first
                _logger.LogInformation("🔄 Testing PayMob connection...");
                try
                {
                    var testToken = await _payMobService.GetAuthTokenAsync();
                    if (string.IsNullOrEmpty(testToken))
                    {
                        throw new Exception("Auth token is null or empty");
                    }
                    _logger.LogInformation("✅ PayMob connection test successful");
                }
                catch (Exception testEx)
                {
                    _logger.LogError(testEx, "❌ PayMob connection test failed");
                    TempData["Error"] = $"Payment gateway connection failed: {testEx.Message}";
                    return RedirectToAction("PayForCourse", new { courseId });
                }

                // Start PayMob payment
                _logger.LogInformation("🔄 Calling PayMob StartPaymentAsync...");
                var paymentUrl = await _payMobService.StartPaymentAsync(user, course);

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    _logger.LogError("❌ PayMob returned empty URL");
                    TempData["Error"] = "Payment gateway not available. Please try again.";
                    return RedirectToAction("PayForCourse", new { courseId });
                }

                _logger.LogInformation("✅ PayMob URL received: {PaymentUrl}", paymentUrl);

                // Log the redirect for debugging
                _logger.LogInformation("🔄 Redirecting user to PayMob iframe...");

                // For debugging - show the URL to the user first
                if (HttpContext.Request.Query.ContainsKey("debug"))
                {
                    ViewBag.PaymentUrl = paymentUrl;
                    ViewBag.Course = course;
                    return View("PaymentDebug");
                }

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Critical error in InitiatePayment for CourseId: {CourseId}", courseId);
                TempData["Error"] = $"Payment error: {ex.Message}";
                return RedirectToAction("PayForCourse", new { courseId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(string? payment_id)
        {
            _logger.LogInformation("🎉 Payment Success page - PaymentId: {PaymentId}", payment_id);
            ViewBag.PaymentId = payment_id;
            ViewBag.Message = "Payment successful! You are now enrolled in the course.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(string? payment_id)
        {
            _logger.LogInformation("❌ Payment Cancel page - PaymentId: {PaymentId}", payment_id);
            ViewBag.PaymentId = payment_id;
            ViewBag.Message = "Payment was cancelled. You can try again.";
            return View();
        }

        [HttpGet]
        public IActionResult Error()
        {
            _logger.LogInformation("💥 Payment Error page");
            ViewBag.Message = "An error occurred during payment. Please try again or contact support.";
            return View();
        }

        // Debug endpoint to test PayMob connection
        [HttpGet]
        public async Task<IActionResult> TestPayMobConnection()
        {
            try
            {
                _logger.LogInformation("🔍 Testing PayMob connection...");

                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Json(new { success = false, error = "User not logged in" });
                }

                var userId = int.Parse(userIdString);
                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                // Test PayMob auth token
                var authToken = await _payMobService.GetAuthTokenAsync();

                _logger.LogInformation("✅ PayMob test completed - Token: {HasToken}", !string.IsNullOrEmpty(authToken));

                return Json(new
                {
                    success = true,
                    message = "PayMob connection successful",
                    hasAuthToken = !string.IsNullOrEmpty(authToken),
                    tokenLength = authToken?.Length ?? 0,
                    user = new { user.Id, user.Email, user.FirstName },
                    configuration = new
                    {
                        hasApiKey = !string.IsNullOrEmpty(HttpContext.RequestServices
                            .GetService<IConfiguration>()?["PayMob:ApiKey"]),
                        hasIntegrationId = !string.IsNullOrEmpty(HttpContext.RequestServices
                            .GetService<IConfiguration>()?["PayMob:IntegrationId"]),
                        hasIframeId = !string.IsNullOrEmpty(HttpContext.RequestServices
                            .GetService<IConfiguration>()?["PayMob:IframeId"])
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 PayMob connection test failed");
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet]
        public IActionResult Debug()
        {
            var debugInfo = new
            {
                SessionId = HttpContext.Session.Id,
                UserId = HttpContext.Session.GetString("UserId"),
                UserName = HttpContext.Session.GetString("UserName"),
                IsAvailable = HttpContext.Session.IsAvailable,
                ServerTime = DateTime.Now,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            };

            _logger.LogInformation("🔍 Debug info: {@DebugInfo}", debugInfo);

            ViewBag.DebugInfo = debugInfo;
            return View();
        }
    }
}