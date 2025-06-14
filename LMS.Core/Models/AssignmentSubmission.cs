using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }

        public int UserId { get; set; }

        public string? Content { get; set; }

        public string? FileUrl { get; set; }

        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        public int? Grade { get; set; }

        public string? Feedback { get; set; }

        public DateTime? GradedDate { get; set; }

        public int? GradedByUserId { get; set; }

        // Navigation Properties
        public virtual Assignment Assignment { get; set; }
        public virtual User User { get; set; }
        public virtual User? GradedBy { get; set; }
    }
}
