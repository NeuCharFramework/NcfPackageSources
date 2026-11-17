/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DaprClientOptions.cs

    File description: DaprClientOptions related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr
{
    public class DaprClientOptions
    {
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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

        //Legacy comment translated to English.
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

        //Legacy comment translated to English.
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
