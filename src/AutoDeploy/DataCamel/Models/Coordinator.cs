using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel.Models
{
    public class Coordinator
    {
        public int RpfId { get; set; }
        public string Name { get; set; }
        public string CoordinatorUrl { get; set; }
        public string JobManagerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
