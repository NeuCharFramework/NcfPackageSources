/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFunctionRequest.cs
    File description: MyFunctionRequest related implementation

    Created by: Senparc - 20250325

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.5.10 Remove DatabaseSample and related components from MCP extension

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.MCP.OHS.Local.PL
{
    public class MyFunction_MCPCallRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.ServerSelection")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(McpServerSelectionOptions))]
        public string McpServerSelection { get; set; }

        [JsonIgnore]
        public SelectionList McpServerSelectionOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint")]
        public string Endpoint { get; set; }

        [Required]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Request")]
        public string RequestPrompt { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //Legacy comment translated to English.
            McpServerSelectionOptions.Items.Add(new SelectionItem(
                "Manual",
                McpResource.Get("Parameter.MCP.Manual"),
                McpResource.Get("Parameter.MCP.Manual.Help"),
                true));

            //Legacy comment translated to English.
            var mcpServers = XncfRegisterManager.McpServerInfoCollection.Values.ToList();
            
            foreach (var mcpServer in mcpServers)
            {
                var displayText = McpResource.Format(
                    "Parameter.MCP.Server.Display",
                    "{0}（{1}）",
                    mcpServer.XncfName,
                    mcpServer.McpRoute);
                var description = McpResource.Format(
                    "Parameter.MCP.Server.Help",
                    "服务器：{0}，路由：{1}",
                    mcpServer.ServerName,
                    mcpServer.McpRoute);
                //Legacy comment translated to English.
                var serverKey = $"{mcpServer.XncfName}|{mcpServer.McpRoute}";
                
                McpServerSelectionOptions.Items.Add(new SelectionItem(serverKey, displayText, description));
            }

            await base.LoadData(serviceProvider);
        }
    }
}
