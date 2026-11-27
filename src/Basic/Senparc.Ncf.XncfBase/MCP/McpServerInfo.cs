/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpServerInfo.cs
    File description: McpServerInfo related implementation

    Created by: Senparc - 20250620

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

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
        /// <summary>
        /// Initializes an empty server information collection.
        /// </summary>
        public McpServerInfoCollection() { }

        /// <summary>
        /// Initializes the collection with servers keyed by their server names.
        /// </summary>
        /// <param name="collection">The server records to add.</param>
        public McpServerInfoCollection(IEnumerable<McpServerInfo> collection)
        {
            foreach (var item in collection)
            {
                this.TryAdd(item.ServerName, item);
            }
        }

        /// <summary>
        /// Finds a server record by its exact dictionary key.
        /// </summary>
        /// <param name="serverName">The server name used as the collection key.</param>
        /// <returns>The matching server record, or <see langword="null"/> when absent.</returns>
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
        /// <summary>Gets or sets the owning XNCF module UID.</summary>
        public string XncfUid { get; set; }
        /// <summary>Gets or sets the owning XNCF module name.</summary>
        public string XncfName { get; set; }
        /// <summary>Gets or sets the stable MCP server name.</summary>
        public string ServerName { get; set; }
        /// <summary>Gets or sets the mapped MCP route.</summary>
        public string McpRoute { get; set; }
    }
}
