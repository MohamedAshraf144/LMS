using Microsoft.AspNetCore.Mvc;
using LMS.Core.Interfaces;
using LMS.Core.Models;
using LMS.Application.DTOs;

namespace LMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IRepository<Category> _categoryRepository;

        public CategoriesController(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var activeCategories = categories.Where(c => c.IsActive).ToList();
            return Ok(activeCategories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null || !category.IsActive)
                return NotFound();

            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                IconUrl = dto.IconUrl,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var createdCategory = await _categoryRepository.AddAsync(category);
            return CreatedAtAction(nameof(GetCategory), new { id = createdCategory.Id }, createdCategory);
        }
    }
}