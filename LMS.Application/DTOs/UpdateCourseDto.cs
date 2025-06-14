using LMS.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs
{
    public class UpdateCourseDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public decimal? Price { get; set; }

        public CourseLevel Level { get; set; }

        public CourseStatus Status { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Duration { get; set; }
    }
}
