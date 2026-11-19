/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFuctionAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20211031

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260717
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.5.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261004
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.5.10 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.5.10 add AITokenUsageRecorder and related services for token usage tracking

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

#pragma warning disable OPENAI001 //Legacy comment translated to English.
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Agents.AI;
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
using Senparc.Xncf.AIKernel.Domain.Services;
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
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Tool.Input")]
        public string Str { get; set; }
    }


    [McpServerToolType()]
    public static class NcfMcpTools
    {
        [McpServerTool, LocalizedDescription(typeof(McpResource), "MCP.Tool.Echo.Description")]
        public static string Echo(string message)
        {
            Console.WriteLine("Echo 收到来自 MCP的 请求，Message:" + message);
            return McpResource.Format("MCP.Tool.Echo.Result", "你好，{0}", message);
        }

        [McpServerTool, LocalizedDescription(typeof(McpResource), "MCP.Tool.Now.Description")]
        public static string Now(string message)
        {
            Console.WriteLine("Now tool 收到请求(messag)：" + message);
            return $"{message}: {DateTime.Now}";
        }

        //Legacy comment translated to English.
        //public static string Now(DoFuncReq reqeust)
        //{
        //Legacy comment translated to English.
        //    return $"{reqeust.Str}: {DateTime.Now}";
        //}

        //Legacy comment translated to English.
        [McpServerTool, LocalizedDescription(typeof(McpResource), "MCP.Tool.AddHours.Description")]
        public static string AddHours(int hours)
        {
            return $"{DateTime.Now.AddHours(hours)}";
        }


     [McpServerTool,Description("Bob's Lab")]
    public static async Task<string> BobLab(string input)
    {
        var handler = new AgentAiHandler(Senparc.AI.Config.SenparcAiSetting);

        var iWantToRun = await handler.IWantTo()
            .ConfigChatModel("Bob", new ChatClientAgentOptions(){
            ChatOptions = new ChatOptions()
            {
                Instructions = McpResource.Get("MCP.BobLab.Instructions")
            }
        }).BuildKernelWithAgentSessionAsync();

        var startTime = DateTime.Now;
        var result = await iWantToRun.RunChatAsync(input);
        return result.OutputString + McpResource.Format(
            "MCP.BobLab.Footer",
            "[Bob's Lab] - {0} / {1}，耗时：{2} 毫秒",
            input,
            DateTime.Now,
            SystemTime.DiffTotalMS(startTime));
    }

    }

    [McpServerToolType]
    public class MyFuctionAppService : AppServiceBase
    {
        //private IMcpClient McpClient { get; set; }

        public MyFuctionAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        [FunctionRender(typeof(McpResource), "Function.MCP.Execute.Name", "Function.MCP.Execute.Description", typeof(Register))]
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
                //    Command = "curl http://localhost:5080/sse/sse",
                //    // Arguments = ["-y", "@modelcontextprotocol/server-everything"],
                //});

                //Legacy comment translated to English.
                string endpoint;
                var selectedMcpServer = request.McpServerSelection;

                if (!string.IsNullOrEmpty(selectedMcpServer) && selectedMcpServer != "Manual")
                {
                    //Legacy comment translated to English.
                    var serverParts = selectedMcpServer.Split('|');
                    if (serverParts.Length == 2)
                    {
                        var xncfName = serverParts[0];
                        var mcpRoute = serverParts[1];

                        //Legacy comment translated to English.
                        var mcpServerInfo = Senparc.Ncf.XncfBase.XncfRegisterManager.McpServerInfoCollection.Values
                            .FirstOrDefault(s => s.XncfName == xncfName && s.McpRoute == mcpRoute);

                        if (mcpServerInfo != null)
                        {
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            endpoint = $"http://localhost:5080/{mcpServerInfo.McpRoute}/sse";
                            Console.WriteLine($"使用选中的 MCP 服务器: {mcpServerInfo.XncfName}，路由: {mcpServerInfo.McpRoute}/sse");
                        }
                        else
                        {
                            //Legacy comment translated to English.
                            endpoint = "http://localhost:5080/mcp-senparc-xncf-mcp/sse";
                            Console.WriteLine($"警告：找不到选中的 MCP 服务器信息，使用默认端点");
                        }
                    }
                    else
                    {
                        //Legacy comment translated to English.
                        endpoint = "http://localhost:5080/mcp-senparc-xncf-mcp/sse";
                        Console.WriteLine($"警告：无法解析选中的 MCP 服务器标识: {selectedMcpServer}，使用默认端点");
                    }
                }
                else
                {
                    //Legacy comment translated to English.
                    endpoint = request.Endpoint.IsNullOrEmpty() ? "http://localhost:5080/mcp-senparc-xncf-mcp/sse" : request.Endpoint;
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
                    "Jeffrey", McpResource.Get("MCP.Agent.SystemPrompt"),
                    new ChatOptions()
                    {
                        Instructions = McpResource.Get("MCP.Agent.SystemPrompt"),
                        TopP = 0.7f,
                        Temperature = 0.7f,
                        MaxOutputTokens = 2000,
                        Tools = new List<AITool> { ncfServerTool },
                    });

                var iWantToRun = await iWantToConfig
                        .ConfigChatModel("Jeffrey", chatOptions)
                        .BuildKernelAsync();

                var aiModelService = ServiceProvider.GetService<AIModelService>();
                var resultRaw = aiModelService == null
                    ? await iWantToRun.RunChatAsync(request.RequestPrompt)
                    : await aiModelService.RunChatWithUsageAsync(
                        iWantToRun,
                        request.RequestPrompt,
                        modelAlias: "MCP.MyFunctionAppService",
                        source: "MCP.MyFunctionAppService");
                //return resultRaw.ToJson(true);
                return resultRaw.OutputString;
            });
        }

        [McpServerTool, LocalizedDescription(typeof(McpResource), "MCP.Tool.Calculator.Description")]
        public async Task<string> Calculator(
            int number1, int number2, int power, [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Tool.Operator")] string operatorMark
            //RequestType request
            )
        {
            /* Legacy comment translated to English.

              *

              * parametersdescription：

              * Legacy comment translated to English.

              * Legacy comment translated to English.

              * 

              * Legacy comment translated to English.

              * Legacy comment translated to English.

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
                        //Legacy comment translated to English.
                        return McpResource.Get("MCP.Calculate.DivideByZero");
                    }
                    calcResult = calcResult / request.Number2;
                    break;
                default:
                    //response.Success = false;
                    //Legacy comment translated to English.
                    return McpResource.Format("MCP.Calculate.UnknownOperator", "未知的运算符：{0}", request.TheOperator);
            }

            //Legacy comment translated to English.

            Action<int> raisePower = p =>
            {
                if (request.Power.ToString().Contains(p.ToString()))
                {
                    if (request.Power == p)
                    {
                        var oldValue = calcResult;
                        calcResult = Math.Pow(calcResult, request.Power);
                        //Legacy comment translated to English.
                    }
                }
            };

            raisePower(2);
            raisePower(3);

            //Legacy comment translated to English.
            return McpResource.Format("MCP.Calculate.Result", "【{0}】计算结果：{1}。计算过程请查看日志", request.TheOperator, calcResult);
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
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Tool.Operator")]
        public string TheOperator { get; set; }

    }
}
#pragma warning restore OPENAI001 //Legacy comment translated to English.
