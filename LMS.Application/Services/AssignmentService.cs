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
    public class AssignmentService : IAssignmentService
    {
        private readonly IRepository<Assignment> _assignmentRepository;
        private readonly IRepository<AssignmentSubmission> _submissionRepository;

        public AssignmentService(
            IRepository<Assignment> assignmentRepository,
            IRepository<AssignmentSubmission> submissionRepository)
        {
            _assignmentRepository = assignmentRepository;
            _submissionRepository = submissionRepository;
        }

        public async Task<Assignment?> GetAssignmentByIdAsync(int id)
        {
            return await _assignmentRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByCourseAsync(int courseId)
        {
            return await _assignmentRepository.FindAsync(a => a.CourseId == courseId);
        }

        public async Task<Assignment> CreateAssignmentAsync(Assignment assignment)
        {
            assignment.CreatedDate = DateTime.UtcNow;
            return await _assignmentRepository.AddAsync(assignment);
        }

        public async Task UpdateAssignmentAsync(Assignment assignment)
        {
            await _assignmentRepository.UpdateAsync(assignment);
        }

        public async Task DeleteAssignmentAsync(int id)
        {
            await _assignmentRepository.DeleteAsync(id);
        }

        public async Task<AssignmentSubmission> SubmitAssignmentAsync(AssignmentSubmission submission)
        {
            submission.SubmittedDate = DateTime.UtcNow;
            return await _submissionRepository.AddAsync(submission);
        }

        public async Task<IEnumerable<AssignmentSubmission>> GetSubmissionsByAssignmentAsync(int assignmentId)
        {
            return await _submissionRepository.FindAsync(s => s.AssignmentId == assignmentId);
        }

        public async Task GradeSubmissionAsync(int submissionId, int grade, string feedback, int gradedByUserId)
        {
            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission != null)
            {
                submission.Grade = grade;
                submission.Feedback = feedback;
                submission.GradedByUserId = gradedByUserId;
                submission.GradedDate = DateTime.UtcNow;
                await _submissionRepository.UpdateAsync(submission);
            }
        }
    }
}
