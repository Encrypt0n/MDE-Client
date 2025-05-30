using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDE_Client.Domain.Models
{
    
        public class Company
        {
            public int CompanyID { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Subnet { get; set; }

        public string Description { get; set; }
        }
    

}
