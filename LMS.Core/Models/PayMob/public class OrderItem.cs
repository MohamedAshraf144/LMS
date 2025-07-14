using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class OrderItem
    {
        public string name { get; set; } = string.Empty;
        public int amount_cents { get; set; }
        public string description { get; set; } = string.Empty;
        public int quantity { get; set; } = 1;
    }
}
