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
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IRepository<Enrollment> _enrollmentRepository;
        private readonly IRepository<Course> _courseRepository;
        private readonly IRepository<LessonProgress> _lessonProgressRepository;

        public EnrollmentService(
            IRepository<Enrollment> enrollmentRepository,
            IRepository<Course> courseRepository,
            IRepository<LessonProgress> lessonProgressRepository)
        {
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _lessonProgressRepository = lessonProgressRepository;
        }

        public async Task<Enrollment> EnrollUserInCourseAsync(int userId, int courseId)
        {
            // Check if user is already enrolled
            var existingEnrollments = await _enrollmentRepository.FindAsync(e =>
                e.UserId == userId && e.CourseId == courseId);

            if (existingEnrollments.Any())
            {
                throw new InvalidOperationException("User is already enrolled in this course");
            }

            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = courseId,
                EnrollmentDate = DateTime.UtcNow,
                Status = EnrollmentStatus.Active,
                Progress = 0
            };

            return await _enrollmentRepository.AddAsync(enrollment);
        }

        public async Task<IEnumerable<Enrollment>> GetUserEnrollmentsAsync(int userId)
        {
            return await _enrollmentRepository.FindAsync(e => e.UserId == userId);
        }

        public async Task UpdateProgressAsync(int enrollmentId, decimal progress)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
            if (enrollment != null)
            {
                enrollment.Progress = progress;
                await _enrollmentRepository.UpdateAsync(enrollment);
            }
        }

        public async Task CompleteCourseAsync(int enrollmentId)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
            if (enrollment != null)
            {
                enrollment.Status = EnrollmentStatus.Completed;
                enrollment.Progress = 100;
                enrollment.CompletionDate = DateTime.UtcNow;
                await _enrollmentRepository.UpdateAsync(enrollment);
            }
        }

        public async Task<decimal> CalculateCourseProgressAsync(int enrollmentId)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
            if (enrollment == null) return 0;

            var course = await _courseRepository.GetByIdAsync(enrollment.CourseId);
            if (course == null || !course.Lessons.Any()) return 0;

            var completedLessons = await _lessonProgressRepository.FindAsync(lp =>
                lp.EnrollmentId == enrollmentId && lp.IsCompleted);

            var progress = (decimal)completedLessons.Count() / course.Lessons.Count * 100;

            await UpdateProgressAsync(enrollmentId, progress);

            return progress;
        }
    }
}
