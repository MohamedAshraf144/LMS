using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class OrderRequest
    {
        public string auth_token { get; set; } = string.Empty;
        public int delivery_needed { get; set; } = 0;
        public int amount_cents { get; set; }
        public string currency { get; set; } = "EGP";
        public List<OrderItem> items { get; set; } = new();
    }

}
