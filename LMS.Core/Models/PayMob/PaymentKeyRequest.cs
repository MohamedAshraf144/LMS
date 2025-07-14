using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class PaymentKeyRequest
    {
        public string auth_token { get; set; } = string.Empty;
        public int amount_cents { get; set; }
        public int expiration { get; set; } = 3600;
        public int order_id { get; set; }
        public BillingData billing_data { get; set; } = new();
        public string currency { get; set; } = "EGP";
        public string integration_id { get; set; } = string.Empty;
    }


}
