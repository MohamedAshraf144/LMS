// LMS.Web/Controllers/CategoriesController.cs
using LMS.Core.Interfaces;
using LMS.Core.Interfaces.Services;
using LMS.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Web.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly ICourseService _courseService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            IRepository<Category> categoryRepository,
            ICourseService courseService,
            ILogger<CategoriesController> logger)
        {
            _categoryRepository = categoryRepository;
            _courseService = courseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                var activeCategories = categories.Where(c => c.IsActive).ToList();

                // Get course count for each category
                var courses = await _courseService.GetPublishedCoursesAsync();
                foreach (var category in activeCategories)
                {
                    category.Courses = courses.Where(c => c.CategoryId == category.Id).ToList();
                }

                return View(activeCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories page");
                TempData["Error"] = "Error loading categories";
                return View(new List<Category>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null || !category.IsActive)
                {
                    TempData["Error"] = "Category not found";
                    return RedirectToAction(nameof(Index));
                }

                return RedirectToAction("Index", "Courses", new { categoryId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category details for ID: {CategoryId}", id);
                TempData["Error"] = "Error loading category details";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}