using Microsoft.AspNetCore.Mvc;
using LMS.Core.Interfaces.Services;

namespace LMS.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICourseService _courseService;

        public HomeController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _courseService.GetPublishedCoursesAsync();
            return View(courses);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}