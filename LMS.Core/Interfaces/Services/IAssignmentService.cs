using LMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Interfaces.Services
{
    public interface IAssignmentService
    {
        Task<Assignment?> GetAssignmentByIdAsync(int id);
        Task<IEnumerable<Assignment>> GetAssignmentsByCourseAsync(int courseId);
        Task<Assignment> CreateAssignmentAsync(Assignment assignment);
        Task UpdateAssignmentAsync(Assignment assignment);
        Task DeleteAssignmentAsync(int id);
        Task<AssignmentSubmission> SubmitAssignmentAsync(AssignmentSubmission submission);
        Task<IEnumerable<AssignmentSubmission>> GetSubmissionsByAssignmentAsync(int assignmentId);
        Task GradeSubmissionAsync(int submissionId, int grade, string feedback, int gradedByUserId);
    }
}
