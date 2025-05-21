using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDE_Client.Domain.Models
{
    public class UserActivityLog
    {
        public int UserId { get; set; }
        public int MachineId { get; set; }
        public string Action { get; set; } = default!;
        public string? Target { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

}
