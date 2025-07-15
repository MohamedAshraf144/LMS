using System.Collections.Generic;
using LMS.Core.Models;

namespace LMS.Web.Models
{
    public class ProfileViewModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<Course> Courses { get; set; } = new();
    }
} 