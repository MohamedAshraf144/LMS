using LMS.Application.DTOs;
using LMS.Core.Interfaces.Services;
using LMS.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly IEnrollmentService _enrollmentService;

        public CoursesController(ICourseService courseService, IEnrollmentService enrollmentService)
        {
            _courseService = courseService;
            _enrollmentService = enrollmentService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _courseService.GetPublishedCoursesAsync();
            return Ok(courses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            return Ok(course);
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                InstructorId = userId,
                CategoryId = dto.CategoryId,
                Price = dto.Price,
                Level = dto.Level,
                Duration = dto.Duration
            };

            var createdCourse = await _courseService.CreateCourseAsync(course);
            return CreatedAtAction(nameof(GetCourse), new { id = createdCourse.Id }, createdCourse);
        }

        [HttpPost("{id}/enroll")]
        public async Task<IActionResult> EnrollInCourse(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var enrollment = await _enrollmentService.EnrollUserInCourseAsync(userId, id);
                return Ok(enrollment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var enrollments = await _enrollmentService.GetUserEnrollmentsAsync(userId);
            return Ok(enrollments);
        }

        [HttpGet("instructor/{instructorId}")]
        public async Task<IActionResult> GetCoursesByInstructor(int instructorId)
        {
            var courses = await _courseService.GetCoursesByInstructorAsync(instructorId);
            return Ok(courses);
        }
    }
}
