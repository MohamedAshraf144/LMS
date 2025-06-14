using LMS.Core.Enums;
using LMS.Core.Interfaces;
using LMS.Core.Interfaces.Services;
using LMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.Services
{
    public class CourseService : ICourseService
    {
        private readonly IRepository<Course> _courseRepository;
        private readonly IRepository<Enrollment> _enrollmentRepository;

        public CourseService(IRepository<Course> courseRepository, IRepository<Enrollment> enrollmentRepository)
        {
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
        }

        public async Task<Course?> GetCourseByIdAsync(int id)
        {
            return await _courseRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Course>> GetCoursesByInstructorAsync(int instructorId)
        {
            return await _courseRepository.FindAsync(c => c.InstructorId == instructorId);
        }

        public async Task<IEnumerable<Course>> GetPublishedCoursesAsync()
        {
            return await _courseRepository.FindAsync(c => c.Status == CourseStatus.Published);
        }

        public async Task<Course> CreateCourseAsync(Course course)
        {
            course.CreatedDate = DateTime.UtcNow;
            course.Status = CourseStatus.Draft;
            return await _courseRepository.AddAsync(course);
        }

        public async Task UpdateCourseAsync(Course course)
        {
            await _courseRepository.UpdateAsync(course);
        }

        public async Task DeleteCourseAsync(int id)
        {
            await _courseRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm)
        {
            return await _courseRepository.FindAsync(c =>
                c.Title.Contains(searchTerm) ||
                c.Description.Contains(searchTerm));
        }

        public async Task<IEnumerable<Course>> GetCoursesByCategoryAsync(int categoryId)
        {
            return await _courseRepository.FindAsync(c =>
                c.CategoryId == categoryId &&
                c.Status == CourseStatus.Published);
        }
    }
}
