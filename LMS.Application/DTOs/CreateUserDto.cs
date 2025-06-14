using LMS.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs
{
    public class CreateUserDto
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Phone { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        public UserRole Role { get; set; } = UserRole.Student;
    }
}
