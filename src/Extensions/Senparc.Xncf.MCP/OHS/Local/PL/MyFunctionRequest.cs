using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.MCP.OHS.Local.PL
{
    public class MyFunction_MCPCallRequest : FunctionAppRequestBase
    {
        [Description("MCP 服务器选择||选择已注册的 MCP 服务器，或选择 手动输入 自定义服务器地址")]
        public SelectionList McpServerSelection { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [Description("MCP 服务器地址||MCP 服务器地址，当选择 手动输入 时需要填写，默认为 http://localhost:5000/mcp-senparc-xncf-mcp/sse")]
        public string Endpoint { get; set; }

        [Required]
        [Description("请求||提出对 MCP 服务器的请求")]
        public string RequestPrompt { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            // 添加手动输入选项
            McpServerSelection.Items.Add(new SelectionItem("Manual", "手动输入", "手动输入 MCP 服务器地址", true));

            // 从 XncfRegisterManager 获取已注册的 MCP 服务器
            var mcpServers = XncfRegisterManager.McpServerInfoCollection.Values.ToList();
            
            foreach (var mcpServer in mcpServers)
            {
                var displayText = $"{mcpServer.XncfName}";
                var description = $"服务器：{mcpServer.ServerName}，路由：{mcpServer.McpRoute}";
                var endpoint = $"/{mcpServer.McpRoute}"; // 构建完整的端点地址
                
                McpServerSelection.Items.Add(new SelectionItem(endpoint, displayText, description));
            }

            await base.LoadData(serviceProvider);
        }
    }
    public class MyFunction_CaculateRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [Description("名称||双竖线之前为参数名称，双竖线之后为参数注释")]
        public string Name { get; set; }

        [Required]
        [Description("数字||数字1")]
        public int Number1 { get; set; }


        [Required]
        [Description("数字||数字2")]
        public int Number2 { get; set; }

        [Description("运算符||")]//下拉列表
        public SelectionList Operator { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+","加法","数字1 + 数字2",false),
                 new SelectionItem("-","减法","数字1 - 数字2",true),
                 new SelectionItem("×","乘法","数字1 × 数字2",false),
                 new SelectionItem("÷","除法","数字1 ÷ 数字2",false)
            });

        [Description("计算平方||")]//多选框
        public SelectionList Power { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2","平方","计算上述结果之后再计算平方",false),
                 new SelectionItem("3","三次方","计算上述结果之后再计算三次方",false)
            });
    }
}
