using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Models
{
    public class ConnectionFactorySetting
    {
        public required string HostName { get; set; }
        public required int Port { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}
