using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Models
{
    public class Invoice
    {
        public Invoice(int number, string serie, string total)
        {
            Number = number;
            Serie = serie;
            Total = total;
        }

        public int Number { get; init; }
        public string Serie { get; init; }
        public string Total { get; init; } 
       
    }
}
