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
    public class LessonService : ILessonService
    {
        private readonly IRepository<Lesson> _lessonRepository;
        private readonly IRepository<LessonProgress> _lessonProgressRepository;

        public LessonService(IRepository<Lesson> lessonRepository, IRepository<LessonProgress> lessonProgressRepository)
        {
            _lessonRepository = lessonRepository;
            _lessonProgressRepository = lessonProgressRepository;
        }

        public async Task<Lesson?> GetLessonByIdAsync(int id)
        {
            return await _lessonRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Lesson>> GetLessonsByCourseAsync(int courseId)
        {
            var lessons = await _lessonRepository.FindAsync(l => l.CourseId == courseId && l.IsPublished);
            return lessons.OrderBy(l => l.OrderIndex);
        }

        public async Task<Lesson> CreateLessonAsync(Lesson lesson)
        {
            lesson.CreatedDate = DateTime.UtcNow;
            return await _lessonRepository.AddAsync(lesson);
        }

        public async Task UpdateLessonAsync(Lesson lesson)
        {
            await _lessonRepository.UpdateAsync(lesson);
        }

        public async Task DeleteLessonAsync(int id)
        {
            await _lessonRepository.DeleteAsync(id);
        }

        public async Task<bool> MarkLessonAsCompletedAsync(int lessonId, int enrollmentId)
        {
            var existingProgress = await _lessonProgressRepository.FindAsync(lp =>
                lp.LessonId == lessonId && lp.EnrollmentId == enrollmentId);

            var progress = existingProgress.FirstOrDefault();

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    LessonId = lessonId,
                    EnrollmentId = enrollmentId,
                    IsCompleted = true,
                    CompletedDate = DateTime.UtcNow
                };
                await _lessonProgressRepository.AddAsync(progress);
            }
            else if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedDate = DateTime.UtcNow;
                await _lessonProgressRepository.UpdateAsync(progress);
            }

            return true;
        }

        public async Task<LessonProgress?> GetLessonProgressAsync(int lessonId, int enrollmentId)
        {
            var progresses = await _lessonProgressRepository.FindAsync(lp =>
                lp.LessonId == lessonId && lp.EnrollmentId == enrollmentId);
            return progresses.FirstOrDefault();
        }
    }
}
