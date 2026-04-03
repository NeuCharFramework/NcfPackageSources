using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder
{
    public class Request
    {
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? Body { get; set; }

        // Add new fields to test dynamic updates
        public Dictionary<string, string>? Headers { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

}
