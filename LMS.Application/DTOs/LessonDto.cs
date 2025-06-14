using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs
{
    public class LessonDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Content { get; set; }
        public string? VideoUrl { get; set; }
        public int Duration { get; set; }
        public int OrderIndex { get; set; }
        public int CourseId { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsCompleted { get; set; }
    }
}
