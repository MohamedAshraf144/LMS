using LMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Interfaces.Services
{
    public interface ICourseService
    {
        Task<Course?> GetCourseByIdAsync(int id);
        Task<IEnumerable<Course>> GetCoursesByInstructorAsync(int instructorId);
        Task<IEnumerable<Course>> GetPublishedCoursesAsync();
        Task<Course> CreateCourseAsync(Course course);
        Task UpdateCourseAsync(Course course);
        Task DeleteCourseAsync(int id);
    }
}
