using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class WebhookData
    {
        public PaymentTransaction obj { get; set; } = new();
        public string type { get; set; } = string.Empty;
    }
}
