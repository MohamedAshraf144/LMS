using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Core.Models.PayMob
{
    public class BillingData
    {
        public string apartment { get; set; } = "NA";
        public string email { get; set; } = string.Empty;
        public string floor { get; set; } = "NA";
        public string first_name { get; set; } = string.Empty;
        public string street { get; set; } = "NA";
        public string building { get; set; } = "NA";
        public string phone_number { get; set; } = string.Empty;
        public string shipping_method { get; set; } = "NA";
        public string postal_code { get; set; } = "NA";
        public string city { get; set; } = "NA";
        public string country { get; set; } = "EG";
        public string last_name { get; set; } = string.Empty;
        public string state { get; set; } = "NA";
    }

}
