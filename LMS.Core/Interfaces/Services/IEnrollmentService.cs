using LMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Interfaces.Services
{
    public interface IEnrollmentService
    {
        Task<Enrollment> EnrollUserInCourseAsync(int userId, int courseId);
        Task<IEnumerable<Enrollment>> GetUserEnrollmentsAsync(int userId);
        Task UpdateProgressAsync(int enrollmentId, decimal progress);
        Task CompleteCourseAsync(int enrollmentId);
    }
}
