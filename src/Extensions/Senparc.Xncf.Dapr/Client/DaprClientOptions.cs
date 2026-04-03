using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr
{
    public class DaprClientOptions
    {
        //Dapr Apilistening port
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
                    throw new Exception("Invalid port number");
                }
            }
        }
        //State storage component name
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

        //Publish and subscribe component name
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

        //DaprMaximum number of reconnect attempts
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
