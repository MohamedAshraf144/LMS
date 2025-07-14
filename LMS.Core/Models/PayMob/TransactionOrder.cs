using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class TransactionOrder
    {
        public int id { get; set; }
        public string merchant_order_id { get; set; } = string.Empty;
    }
}
