using System.ComponentModel.DataAnnotations;

namespace LMS.Core.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Content { get; set; }

        public string? VideoUrl { get; set; }

        public int Duration { get; set; } // in minutes

        public int OrderIndex { get; set; }

        public int CourseId { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Course Course { get; set; }
        public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    }
}