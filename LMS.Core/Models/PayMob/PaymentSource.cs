using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class PaymentSource
    {
        public string type { get; set; } = string.Empty;
        public string pan { get; set; } = string.Empty;
        public string sub_type { get; set; } = string.Empty;
    }
}
