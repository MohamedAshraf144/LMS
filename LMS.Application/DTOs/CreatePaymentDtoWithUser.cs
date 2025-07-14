using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs
{
    public class CreatePaymentDtoWithUser
    {
        public int CourseId { get; set; }
        public int UserId { get; set; }
    }
}
