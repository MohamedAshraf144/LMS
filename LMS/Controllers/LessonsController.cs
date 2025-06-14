using LMS.Application.DTOs;
using LMS.Core.Interfaces.Services;
using LMS.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetLessonsByCourse(int courseId)
        {
            var lessons = await _lessonService.GetLessonsByCourseAsync(courseId);
            return Ok(lessons);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLesson(int id)
        {
            var lesson = await _lessonService.GetLessonByIdAsync(id);
            if (lesson == null)
                return NotFound();

            return Ok(lesson);
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteLesson(int id, [FromBody] int enrollmentId)
        {
            var result = await _lessonService.MarkLessonAsCompletedAsync(id, enrollmentId);
            if (result)
                return Ok(new { Message = "Lesson marked as completed" });

            return BadRequest("Failed to mark lesson as completed");
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var lesson = new Lesson
            {
                Title = dto.Title,
                Description = dto.Description,
                Content = dto.Content,
                VideoUrl = dto.VideoUrl,
                Duration = dto.Duration,
                OrderIndex = dto.OrderIndex,
                CourseId = dto.CourseId,
                IsPublished = dto.IsPublished
            };

            var createdLesson = await _lessonService.CreateLessonAsync(lesson);
            return CreatedAtAction(nameof(GetLesson), new { id = createdLesson.Id }, createdLesson);
        }
    }
}
