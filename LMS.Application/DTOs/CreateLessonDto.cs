using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs
{
    public class CreateLessonDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Content { get; set; }

        public string? VideoUrl { get; set; }

        [Required]
        [Range(1, 300)]
        public int Duration { get; set; }

        [Required]
        public int OrderIndex { get; set; }

        [Required]
        public int CourseId { get; set; }

        public bool IsPublished { get; set; } = false;
    }

}
