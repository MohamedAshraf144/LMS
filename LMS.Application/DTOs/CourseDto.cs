using LMS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs
{
    public class CourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int InstructorId { get; set; }
        public string InstructorName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal? Price { get; set; }
        public CourseLevel Level { get; set; }
        public CourseStatus Status { get; set; }
        public int Duration { get; set; }
        public DateTime CreatedDate { get; set; }
        public int TotalLessons { get; set; }
        public int EnrolledStudents { get; set; }
        public decimal AverageRating { get; set; }
    }

}
