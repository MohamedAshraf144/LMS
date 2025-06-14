using LMS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace LMS.Core.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public int InstructorId { get; set; }

        public int CategoryId { get; set; }

        public decimal? Price { get; set; }

        public CourseLevel Level { get; set; }

        public CourseStatus Status { get; set; }

        public int Duration { get; set; } // in hours

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? PublishedDate { get; set; }

        // Navigation Properties
        public virtual User Instructor { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}