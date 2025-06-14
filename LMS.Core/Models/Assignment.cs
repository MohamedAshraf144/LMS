using LMS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace LMS.Core.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Instructions { get; set; }

        public int CourseId { get; set; }

        public int CreatedByUserId { get; set; }

        public DateTime DueDate { get; set; }

        public int MaxPoints { get; set; }

        public AssignmentType Type { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Course Course { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }
}