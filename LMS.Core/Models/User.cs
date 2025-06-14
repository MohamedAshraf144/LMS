using LMS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace LMS.Core.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Phone { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        // Navigation Properties
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
    }
}