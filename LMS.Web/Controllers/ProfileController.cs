using LMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using LMS.Web.Models;

namespace LMS.Web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ICourseService _courseService;

        public ProfileController(IUserService userService, IEnrollmentService enrollmentService, ICourseService courseService)
        {
            _userService = userService;
            _enrollmentService = enrollmentService;
            _courseService = courseService;
        }

        public async Task<IActionResult> Index()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["Warning"] = "Please login to view your profile.";
                return RedirectToAction("Login", "Account");
            }
            var userId = int.Parse(userIdString);
            var user = await _userService.GetUserByIdAsync(userId);
            var enrollments = await _enrollmentService.GetUserEnrollmentsAsync(userId);
            var courses = new List<LMS.Core.Models.Course>();
            foreach (var enrollment in enrollments)
            {
                var course = await _courseService.GetCourseByIdAsync(enrollment.CourseId);
                if (course != null)
                    courses.Add(course);
            }
            var model = new ProfileViewModel
            {
                DisplayName = ((user?.FirstName ?? "") + " " + (user?.LastName ?? "")).Trim(),
                Email = user?.Email ?? "No Email",
                Courses = courses
            };
            return View(model);
        }
    }
} 