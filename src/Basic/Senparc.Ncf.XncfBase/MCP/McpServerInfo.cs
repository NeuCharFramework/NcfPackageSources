/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：McpServerInfo.cs
    文件功能描述：McpServerInfo 相关实现
    
    
    创建标识：Senparc - 20250620
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
