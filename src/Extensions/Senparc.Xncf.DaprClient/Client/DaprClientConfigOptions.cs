using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DaprClient.Client
{
    public class DaprClientConfigOptions
    {
        public int HttpApiPort
        {
            get
            {
                return HttpApiPort;
            }
            set
            {
                if (value > 0 & value < 65536)
                {
                    HttpApiPort = value;
                }
                else
                {
                    throw new Exception("无效的端口号");
                }
            }
        }
        public string? StateStoreName { get; set; }
        public string? PubSubName { get; set; }
    }
}
