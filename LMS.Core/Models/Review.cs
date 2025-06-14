using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CourseId { get; set; }

        public int Rating { get; set; } // 1-5

        public string? Comment { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual Course Course { get; set; }
    }
}
