using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models
{
    public class LessonProgress
    {
        public int Id { get; set; }

        public int EnrollmentId { get; set; }

        public int LessonId { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedDate { get; set; }

        public int WatchTime { get; set; } = 0; // in seconds

        // Navigation Properties
        public virtual Enrollment Enrollment { get; set; }
        public virtual Lesson Lesson { get; set; }
    }
}
