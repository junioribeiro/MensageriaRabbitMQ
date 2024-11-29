using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Models
{
    public class Payment
    {
        public Order? Order { get; set; }

        public string? PaymentGateway { get; set; }

        public double Amount { get; set; }

        public PaymentStatus Status { get; set; }
    }
    public enum PaymentStatus
    {
        Success,
        Failed,
        Aborted
    }
}
