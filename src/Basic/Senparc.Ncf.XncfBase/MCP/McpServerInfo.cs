/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpServerInfo.cs
    File description: McpServerInfo related implementation

    Created by: Senparc - 20250620

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.MCP
{

    public class McpServerInfoCollection : ConcurrentDictionary<string, McpServerInfo>
    {
        public McpServerInfoCollection() { }

        public McpServerInfoCollection(IEnumerable<McpServerInfo> collection)
        {
            foreach (var item in collection)
            {
                this.TryAdd(item.ServerName, item);
            }
        }

        public McpServerInfo GetByServerName(string serverName)
        {
            if (base.ContainsKey(serverName))
            {
                return base[serverName];
            }

            return null;
        }
    }

    public class McpServerInfo
    {
        public string XncfUid { get; set; }
        public string XncfName { get; set; }
        public string ServerName { get; set; }
        public string McpRoute { get; set; }
    }
}
