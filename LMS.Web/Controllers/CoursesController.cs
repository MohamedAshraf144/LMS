// LMS.Web/Controllers/CoursesController.cs
using Microsoft.AspNetCore.Mvc;
using LMS.Core.Interfaces.Services;
using LMS.Core.Models;
using LMS.Shared.Helpers;
using LMS.Core.Interfaces;
using LMS.Core.Interfaces.Services;
namespace LMS.Web.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly IRepository<Category> _categoryRepository;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(
            ICourseService courseService,
            IEnrollmentService enrollmentService,
            IRepository<Category> categoryRepository,
            ILogger<CoursesController> logger)
        {
            _courseService = courseService;
            _enrollmentService = enrollmentService;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12, string? search = null, int? categoryId = null, int? level = null)
        {
            try
            {
                var courses = await _courseService.GetPublishedCoursesAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    courses = courses.Where(c =>
                        c.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (c.Description != null && c.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                    );
                }

                if (categoryId.HasValue)
                {
                    courses = courses.Where(c => c.CategoryId == categoryId.Value);
                }

                if (level.HasValue)
                {
                    courses = courses.Where(c => (int)c.Level == level.Value);
                }

                // Pagination
                var paginatedCourses = PaginationHelper.CreatePaginatedResult(
                    courses.OrderByDescending(c => c.CreatedDate),
                    page,
                    pageSize
                );

                // Load categories for filter dropdown
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = categories.Where(c => c.IsActive).ToList();
                ViewBag.Search = search;
                ViewBag.CategoryId = categoryId;
                ViewBag.Level = level;

                return View(paginatedCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses page");
                TempData["Error"] = "Error loading courses";
                return View(new PaginatedResult<Course>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                if (course == null)
                {
                    TempData["Error"] = "Course not found";
                    return RedirectToAction(nameof(Index));
                }

                bool isEnrolled = false;
                var userIdString = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdString))
                {
                    var userId = int.Parse(userIdString);
                    var enrollments = await _enrollmentService.GetUserEnrollmentsAsync(userId);
                    isEnrolled = enrollments.Any(e => e.CourseId == id);
                }
                ViewBag.IsEnrolled = isEnrolled;

                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course details for ID: {CourseId}", id);
                TempData["Error"] = "Error loading course details";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Enroll(int courseId)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    TempData["Warning"] = "Please login to enroll in courses";
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdString);
                await _enrollmentService.EnrollUserInCourseAsync(userId, courseId);

                TempData["Success"] = "Successfully enrolled in the course!";
                return RedirectToAction("Details", new { id = courseId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Warning"] = ex.Message;
                return RedirectToAction("Details", new { id = courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user in course {CourseId}", courseId);
                TempData["Error"] = "Error enrolling in course";
                return RedirectToAction("Details", new { id = courseId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index), new { search = query });
        }

        [HttpGet]
        public async Task<IActionResult> Category(int id)
        {
            return RedirectToAction(nameof(Index), new { categoryId = id });
        }
    }
}