using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class OrderResponse
    {
        public int id { get; set; }
        public int amount_cents { get; set; }
        public string currency { get; set; } = string.Empty;
    }
}
