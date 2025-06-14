using LMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Interfaces.Services
{
    public interface ILessonService
    {
        Task<Lesson?> GetLessonByIdAsync(int id);
        Task<IEnumerable<Lesson>> GetLessonsByCourseAsync(int courseId);
        Task<Lesson> CreateLessonAsync(Lesson lesson);
        Task UpdateLessonAsync(Lesson lesson);
        Task DeleteLessonAsync(int id);
        Task<bool> MarkLessonAsCompletedAsync(int lessonId, int enrollmentId);
        Task<LessonProgress?> GetLessonProgressAsync(int lessonId, int enrollmentId);
    }
}
