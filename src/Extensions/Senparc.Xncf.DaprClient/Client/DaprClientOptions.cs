using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DaprClient
{
    public class DaprClientOptions
    {
        //Dapr Api监听端口
        private int httpApiPort;
        public int HttpApiPort
        {
            get
            {
                return httpApiPort;
            }
            set
            {
                if (value > 0 & value < 65536)
                {
                    httpApiPort = value;
                }
                else
                {
                    throw new Exception("无效的端口号");
                }
            }
        }
        //状态存储组件名称
        public string? StateStoreName { get; set; }

        //发布订阅组件名称
        public string? PubSubName { get; set; }

        //Dapr重连最大尝试次数
        private int daprConnectionRetryCount;
        public int DaprConnectionRetryCount
        {
            get { return daprConnectionRetryCount; }
            set
            {
                if(value > 0)
                    daprConnectionRetryCount = value;
            }
        }
    }
}
