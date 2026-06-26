#pragma warning disable OPENAI001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.AI.Entities;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.MCP.Domain.Services;
using Senparc.Xncf.MCP.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Senparc.Xncf.MCP.OHS.Local.AppService
{
    [Serializable]
    public class DoFuncReq
    {
        [Required]
        [Description("传入字符串")]
        public string Str { get; set; }
    }


    [McpServerToolType()]
    public static class NcfMcpTools
    {
        [McpServerTool, Description("处理字符串")]
        public static string Echo(string message)
        {
            Console.WriteLine("Echo 收到来自 MCP的 请求，Message:" + message);
            return $"hello {message}";
        }

        [McpServerTool, Description("获取当前时间")]
        public static string Now(string message)
        {
            Console.WriteLine("Now tool 收到请求(messag)：" + message);
            return $"{message}: {DateTime.Now}";
        }

        //[McpServerTool, Description("获取当前时间")]
        //public static string Now(DoFuncReq reqeust)
        //{
        //    Console.WriteLine("Now tool 收到请求：" + reqeust.ToJson());
        //    return $"{reqeust.Str}: {DateTime.Now}";
        //}

        //自动增加小时数
        [McpServerTool, Description("自动增加小时数")]
        public static string AddHours(int hours)
        {
            return $"{DateTime.Now.AddHours(hours)}";
        }


    }

    [McpServerToolType]
    public class MyFuctionAppService : AppServiceBase
    {
        private ColorService _colorService;
        //private IMcpClient McpClient { get; set; }

        public MyFuctionAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            _colorService = colorService;
        }

        [FunctionRender("执行 MCP", "执行 MCP（如选择模块，默认地址为 http://localhost:5000/{Module Name}/sse）", typeof(Register))]
        public async Task<StringAppResponse> GetMcpResult(MyFunction_MCPCallRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                //Client
                //var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
                //{
                //    Name = "Everything",
                //    Command = "npx",
                //    Arguments = ["-y", "@modelcontextprotocol/server-everything"],
                //});

                //var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
                //{
                //    Name = "NCF-Server",
                //    Command = "curl http://localhost:5000/sse/sse",
                //    // Arguments = ["-y", "@modelcontextprotocol/server-everything"],
                //});

                // 根据 MCP 服务器选择来确定端点
                string endpoint;
                var selectedMcpServer = request.McpServerSelection;

                if (!string.IsNullOrEmpty(selectedMcpServer) && selectedMcpServer != "Manual")
                {
                    // 如果选中了非"手动输入"的 MCP 服务器，从注册列表中获取真实地址
                    var serverParts = selectedMcpServer.Split('|');
                    if (serverParts.Length == 2)
                    {
                        var xncfName = serverParts[0];
                        var mcpRoute = serverParts[1];

                        // 从 XncfRegisterManager 中查找对应的服务器信息
                        var mcpServerInfo = Senparc.Ncf.XncfBase.XncfRegisterManager.McpServerInfoCollection.Values
                            .FirstOrDefault(s => s.XncfName == xncfName && s.McpRoute == mcpRoute);

                        if (mcpServerInfo != null)
                        {
                            // 构建完整的服务器地址
                            endpoint = $"http://localhost:5000/{mcpServerInfo.McpRoute}/sse";
                            Console.WriteLine($"使用选中的 MCP 服务器: {mcpServerInfo.XncfName}，路由: {mcpServerInfo.McpRoute}/sse");
                        }
                        else
                        {
                            // 如果找不到对应的服务器信息，回退到默认地址
                            endpoint = "http://localhost:5000/mcp-senparc-xncf-mcp/sse";
                            Console.WriteLine($"警告：找不到选中的 MCP 服务器信息，使用默认端点");
                        }
                    }
                    else
                    {
                        // 如果解析失败，回退到默认地址
                        endpoint = "http://localhost:5000/mcp-senparc-xncf-mcp/sse";
                        Console.WriteLine($"警告：无法解析选中的 MCP 服务器标识: {selectedMcpServer}，使用默认端点");
                    }
                }
                else
                {
                    // 如果选中了"手动输入"或没有选择，使用手动输入的端点
                    endpoint = request.Endpoint.IsNullOrEmpty() ? "http://localhost:5000/mcp-senparc-xncf-mcp/sse" : request.Endpoint;
                    Console.WriteLine("使用手动输入的端点");
                }

                Console.WriteLine("MCP Request Endpoint:" + endpoint);

                var ncfServerTool = new HostedMcpServerTool("NCF-Server", new Uri(endpoint))
                {
                    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire
                };

                //var clientTransport = new SseClientTransport(new SseClientTransportOptions()
                //{
                //    Endpoint = new Uri(endpoint),
                //    Name = "NCF-Server"
                //});

                //var client = await McpClientFactory.CreateAsync(clientTransport);
                //var tools = await client.ListToolsAsync();
                // Print the list of tools available from the server.



                var aiSetting = Senparc.AI.Config.SenparcAiSetting;
                var agentAiHandler = new AgentAiHandler(aiSetting);

                var iWantToConfig = agentAiHandler.IWantTo();
                var chatOptions = iWantToConfig.CreateChatClientAgentOptions(
                    "Jeffrey", "你是一位智能助手，负责帮助我完成任务",
                    new ChatOptions()
                    {
                        Instructions = "你是一位智能助手，负责帮助我完成任务",
                        TopP = 0.7f,
                        Temperature = 0.7f,
                        MaxOutputTokens = 2000,
                        Tools = new List<AITool> { ncfServerTool },
                    });

                var iWantToRun = await iWantToConfig
                        .ConfigChatModel("Jeffrey", chatOptions)
                        .BuildKernelAsync();

                var resultRaw = await iWantToRun.RunChatAsync(request.RequestPrompt);
                //return resultRaw.ToJson(true);
                return resultRaw.OutputString;
            });
        }



        [FunctionRender("我的函数", "我的函数的注释", typeof(Register))]
        public async Task<StringAppResponse> Calculate(MyFunction_CaculateRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                /* 页面上点击"执行"后，将调用这里的方法
                  *
                  * 参数说明：
                  * response：已经初始化后的返回结果
                  * logger：日志
                  * 
                  * 如果直接对 response 的属性修改，则最终 return null，
                  * 否则可以返回一个新的 response 对象，系统将自动覆盖原有对象
                  */

                double calcResult = request.Number1;
                var theOperator = request.Operator;
                switch (theOperator)
                {
                    case "+":
                        calcResult = calcResult + request.Number2;
                        break;
                    case "-":
                        calcResult = calcResult - request.Number2;
                        break;
                    case "×":
                        calcResult = calcResult * request.Number2;
                        break;
                    case "÷":
                        if (request.Number2 == 0)
                        {
                            response.Success = false;
                            response.ErrorMessage = "被除数不能为0！";
                            return null;
                        }
                        calcResult = calcResult / request.Number2;
                        break;
                    default:
                        response.Success = false;
                        response.ErrorMessage = $"未知的运算符：{theOperator}";
                        return null;
                }

                logger.Append($"进行运算：{request.Number1} {theOperator} {request.Number2} = {calcResult}");

                Action<int> raisePower = power =>
                {
                    if ((request.Power ?? Array.Empty<string>()).Contains(power.ToString()))
                    {
                        var oldValue = calcResult;
                        calcResult = Math.Pow(calcResult, power);
                        logger.Append($"进行{power}次方运算：{oldValue}{(power == 2 ? "²" : "³")} = {calcResult}");
                    }
                };

                raisePower(2);
                raisePower(3);

                response.Data = $"【{request.Name}】计算结果：{calcResult}。计算过程请看日志";
                return null;
            });
        }


        [McpServerTool, Description("计算器工具，负责处理加减乘除计算")]
        public async Task<string> Calculator(
            int number1, int number2, int power, [Description("Number1 和 Number2 之间的运算符，可选：+ - × ÷")] string operatorMark
            //RequestType request
            )
        {
            /* 页面上点击"执行"后，将调用这里的方法
              *
              * 参数说明：
              * response：已经初始化后的返回结果
              * logger：日志
              * 
              * 如果直接对 response 的属性修改，则最终 return null，
              * 否则可以返回一个新的 response 对象，系统将自动覆盖原有对象
              */
            RequestType request = new RequestType
            {
                Power = power,
                Number1 = number1,
                Number2 = number2,
                TheOperator = operatorMark
            };

            Console.WriteLine("收到 MCP 请求：" + request.ToJson(true));

            double calcResult = request.Number1;
            switch (request.TheOperator)
            {
                case "+":
                    calcResult = calcResult + request.Number2;
                    break;
                case "-":
                    calcResult = calcResult - request.Number2;
                    break;
                case "×":
                    calcResult = calcResult * request.Number2;
                    break;
                case "÷":
                    if (request.Number2 == 0)
                    {
                        //response.Success = false;
                        //response.ErrorMessage = "被除数不能为0！";
                        return "被除数不能为0！";
                    }
                    calcResult = calcResult / request.Number2;
                    break;
                default:
                    //response.Success = false;
                    //response.ErrorMessage = $"未知的运算符：{theOperator}";
                    return $"未知的运算符：{request.TheOperator}";
            }

            //logger.Append($"进行运算：{number1} {theOperator} {number2} = {calcResult}");

            Action<int> raisePower = p =>
            {
                if (request.Power.ToString().Contains(p.ToString()))
                {
                    if (request.Power == p)
                    {
                        var oldValue = calcResult;
                        calcResult = Math.Pow(calcResult, request.Power);
                        //logger.Append($"进行{power}次方运算：{oldValue}{(power == 2 ? "²" : "³")} = {calcResult}");
                    }
                }
            };

            raisePower(2);
            raisePower(3);

            // response.Data = $"【{theOperator}】计算结果：{calcResult}。计算过程请看日志";
            return $"【{request.TheOperator}】计算结果：{calcResult}。计算过程请看日志";
            // });
        }
    }

    public class RequestType
    {
        [DefaultValue(1)]
        [Required]
        public int Power { get; set; }

        [DefaultValue(1)]
        [Required]
        public double Number1 { get; set; }

        [DefaultValue(1)]
        [Required]
        public double Number2 { get; set; }

        [Required]
        [DefaultValue("+")]
        [Description("Number1 和 Number2 之间的运算符，可选：+ - × ÷")]
        public string TheOperator { get; set; }

    }
}
#pragma warning restore OPENAI001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
