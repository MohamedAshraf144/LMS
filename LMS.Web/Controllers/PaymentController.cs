// LMS.Web/Controllers/PaymentController.cs
using LMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.Web.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ICourseService _courseService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            ICourseService courseService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _courseService = courseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var payments = await _paymentService.GetUserPaymentsAsync(userId);
                return View(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payments page");
                TempData["Error"] = "حدث خطأ أثناء تحميل صفحة المدفوعات";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> PayForCourse(int courseId)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    TempData["Error"] = "الكورس غير موجود";
                    return RedirectToAction("Index", "Courses");
                }

                if (course.Price == null || course.Price <= 0)
                {
                    TempData["Error"] = "هذا الكورس مجاني";
                    return RedirectToAction("Details", "Courses", new { id = courseId });
                }

                ViewBag.Course = course;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment page for course {CourseId}", courseId);
                TempData["Error"] = "حدث خطأ أثناء تحميل صفحة الدفع";
                return RedirectToAction("Index", "Courses");
            }
        }

        [HttpPost]
        public async Task<IActionResult> InitiatePayment(int courseId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var course = await _courseService.GetCourseByIdAsync(courseId);

                if (course == null)
                {
                    TempData["Error"] = "الكورس غير موجود";
                    return RedirectToAction("Index", "Courses");
                }

                if (course.Price == null || course.Price <= 0)
                {
                    TempData["Error"] = "هذا الكورس مجاني";
                    return RedirectToAction("Details", "Courses", new { id = courseId });
                }

                // Create payment
                var payment = await _paymentService.CreatePaymentAsync(userId, courseId, course.Price.Value);

                // Initiate PayMob payment
                var paymentUrl = await _paymentService.InitiatePaymentAsync(payment.Id);

                // Redirect to PayMob payment page
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment for course {CourseId}", courseId);
                TempData["Error"] = "حدث خطأ أثناء بدء عملية الدفع";
                return RedirectToAction("PayForCourse", new { courseId });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Success(string? payment_id)
        {
            try
            {
                ViewBag.PaymentId = payment_id;
                ViewBag.Message = "تم الدفع بنجاح! سيتم تسجيلك في الكورس خلال دقائق قليلة.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing payment success page");
                return View("Error");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Cancel(string? payment_id)
        {
            try
            {
                ViewBag.PaymentId = payment_id;
                ViewBag.Message = "تم إلغاء عملية الدفع. يمكنك المحاولة مرة أخرى.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing payment cancel page");
                return View("Error");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Error()
        {
            ViewBag.Message = "حدث خطأ أثناء عملية الدفع. يرجى المحاولة مرة أخرى أو التواصل مع الدعم الفني.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var payment = await _paymentService.GetPaymentByIdAsync(id);

                if (payment == null)
                {
                    TempData["Error"] = "عملية الدفع غير موجودة";
                    return RedirectToAction("Index");
                }

                // Ensure user can only view their own payments
                if (payment.UserId != userId && !User.IsInRole("Admin"))
                {
                    TempData["Error"] = "غير مسموح لك بعرض هذه المعاملة";
                    return RedirectToAction("Index");
                }

                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing payment details {PaymentId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل تفاصيل المعاملة";
                return RedirectToAction("Index");
            }
        }
    }
}