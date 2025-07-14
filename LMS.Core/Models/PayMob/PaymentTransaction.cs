using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class PaymentTransaction
    {
        public int id { get; set; }
        public bool pending { get; set; }
        public int amount_cents { get; set; }
        public bool success { get; set; }
        public bool is_auth { get; set; }
        public bool is_capture { get; set; }
        public bool is_standalone_payment { get; set; }
        public bool is_voided { get; set; }
        public bool is_refunded { get; set; }
        public bool is_3d_secure { get; set; }
        public TransactionOrder order { get; set; } = new();
        public string created_at { get; set; } = string.Empty;
        public string currency { get; set; } = string.Empty;
        public PaymentSource source_data { get; set; } = new();
    }
}
