using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr
{
    public class DaprClientOptions
    {
        //Dapr Api监听端口
        private int apiport;
        public int ApiPort
        {
            get
            {
                return apiport;
            }
            set
            {
                if (value > 0 & value < 65536)
                {
                    apiport = value;
                }
                else
                {
                    throw new Exception("无效的端口号");
                }
            }
        }
        //状态存储组件名称
        private string? stateStoreName;
        public string? StateStoreName 
        {
            get
            {
                return stateStoreName;
            }
            set
            {
                if(!string.IsNullOrEmpty(value)) 
                {
                    stateStoreName = value;
                }
            }
        }

        //发布订阅组件名称
        private string? pubSubName;
        public string? PubSubName 
        {
            get
            {
                return pubSubName;
            }
            set
            {
                if(!string.IsNullOrEmpty(value))
                {
                    pubSubName = value;
                }
            }
        }

        //Dapr重连最大尝试次数
        private int daprConnectionRetryCount;
        public int DaprConnectionRetryCount
        {
            get { return daprConnectionRetryCount; }
            set
            {
                if(value > 0)
                    daprConnectionRetryCount = value;
                else
                    daprConnectionRetryCount = 0;
            }
        }
    }
}
