using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Models
{
    public class Order
    {
        public string? UserId { get; set; }

        public string? ProductId { get; set; }

        public double? OrderTotal { get; set; }
    }
}
