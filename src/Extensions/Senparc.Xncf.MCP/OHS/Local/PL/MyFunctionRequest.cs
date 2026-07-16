/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MyFunctionRequest.cs
    文件功能描述：MyFunctionRequest 相关实现
    
    
    创建标识：Senparc - 20250325
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.ServerSelection")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(McpServerSelectionOptions))]
        public string McpServerSelection { get; set; }

        [JsonIgnore]
        public SelectionList McpServerSelectionOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint")]
        public string Endpoint { get; set; }

        [Required]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Request")]
        public string RequestPrompt { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            // 添加手动输入选项
            McpServerSelectionOptions.Items.Add(new SelectionItem(
                "Manual",
                NcfBuiltInResource.Get("Parameter.MCP.Manual"),
                NcfBuiltInResource.Get("Parameter.MCP.Manual.Help"),
                true));

            // 从 XncfRegisterManager 获取已注册的 MCP 服务器
            var mcpServers = XncfRegisterManager.McpServerInfoCollection.Values.ToList();
            
            foreach (var mcpServer in mcpServers)
            {
                var displayText = NcfBuiltInResource.Format(
                    "Parameter.MCP.Server.Display",
                    "{0}（{1}）",
                    mcpServer.XncfName,
                    mcpServer.McpRoute);
                var description = NcfBuiltInResource.Format(
                    "Parameter.MCP.Server.Help",
                    "服务器：{0}，路由：{1}",
                    mcpServer.ServerName,
                    mcpServer.McpRoute);
                // 使用服务器的唯一标识作为 Value，而不是路由
                var serverKey = $"{mcpServer.XncfName}|{mcpServer.McpRoute}";
                
                McpServerSelectionOptions.Items.Add(new SelectionItem(serverKey, displayText, description));
            }

            await base.LoadData(serviceProvider);
        }
    }
    public class MyFunction_CaculateRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Sample.Operator")]//下拉列表
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", NcfBuiltInResource.Get("Parameter.Operator.Add"), NcfBuiltInResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", NcfBuiltInResource.Get("Parameter.Operator.Subtract"), NcfBuiltInResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", NcfBuiltInResource.Get("Parameter.Operator.Multiply"), NcfBuiltInResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", NcfBuiltInResource.Get("Parameter.Operator.Divide"), NcfBuiltInResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Sample.Power")]//多选框
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", NcfBuiltInResource.Get("Parameter.Power.Square"), NcfBuiltInResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", NcfBuiltInResource.Get("Parameter.Power.Cube"), NcfBuiltInResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
