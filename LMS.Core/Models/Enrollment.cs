using LMS.Core.Enums;

namespace LMS.Core.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CourseId { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        public EnrollmentStatus Status { get; set; }

        public decimal Progress { get; set; } = 0;

        public DateTime? CompletionDate { get; set; }

        public decimal? Grade { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual Course Course { get; set; }
        public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    }
}