using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using Senparc.AI.Kernel.Handlers;
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
using ModelContextProtocol.SemanticKernel;
using ModelContextProtocol.SemanticKernel.Extensions;
using Microsoft.SemanticKernel;
using Senparc.AI.Kernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.AI;
using Senparc.AI.Entities;


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

        //[McpServerTool, Description("Get the current time")]
        //public static string Now(DoFuncReq reqeust)
        //{
        //    Console.WriteLine("Now tool received request: " + reqeust.ToJson());
        //    return $"{reqeust.Str}: {DateTime.Now}";
        //}

        //Automatically add hours
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
        private IMcpClient McpClient { get; set; }

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

                // Determine endpoints based on MCP server selection
                string endpoint;
                var selectedMcpServer = request.McpServerSelection.SelectedValues.FirstOrDefault();
                
                if (!string.IsNullOrEmpty(selectedMcpServer) && selectedMcpServer != "Manual")
                {
                    // If an MCP server other than "Manual Entry" is selected, get the real address from the registration list
                    var serverParts = selectedMcpServer.Split('|');
                    if (serverParts.Length == 2)
                    {
                        var xncfName = serverParts[0];
                        var mcpRoute = serverParts[1];
                        
                        // Find the corresponding server information from XncfRegisterManager
                        var mcpServerInfo = Senparc.Ncf.XncfBase.XncfRegisterManager.McpServerInfoCollection.Values
                            .FirstOrDefault(s => s.XncfName == xncfName && s.McpRoute == mcpRoute);
                        
                        if (mcpServerInfo != null)
                        {
                            // Build the complete server address
                            endpoint = $"http://localhost:5000/{mcpServerInfo.McpRoute}/sse";
                            Console.WriteLine($"使用选中的 MCP 服务器: {mcpServerInfo.XncfName}，路由: {mcpServerInfo.McpRoute}/sse");
                        }
                        else
                        {
                            // If the corresponding server information cannot be found, fall back to the default address.
                            endpoint = "http://localhost:5000/mcp-senparc-xncf-mcp/sse";
                            Console.WriteLine($"警告：找不到选中的 MCP 服务器信息，使用默认端点");
                        }
                    }
                    else
                    {
                        // If parsing fails, fall back to the default address
                        endpoint = "http://localhost:5000/mcp-senparc-xncf-mcp/sse";
                        Console.WriteLine($"警告：无法解析选中的 MCP 服务器标识: {selectedMcpServer}，使用默认端点");
                    }
                }
                else
                {
                    // If "Manual input" is checked or not selected, the manually entered endpoint is used
                    endpoint = request.Endpoint.IsNullOrEmpty() ? "http://localhost:5000/mcp-senparc-xncf-mcp/sse" : request.Endpoint;
                    Console.WriteLine("使用手动输入的端点");
                }

                Console.WriteLine("MCP Request Endpoint:" + endpoint);

                var clientTransport = new SseClientTransport(new SseClientTransportOptions()
                {
                    Endpoint = new Uri(endpoint),
                    Name = "NCF-Server"
                });

                var client = await McpClientFactory.CreateAsync(clientTransport);
                var tools = await client.ListToolsAsync();
                // Print the list of tools available from the server.
                foreach (var tool in tools)
                {
                    Console.WriteLine($"{tool.Name} ({tool.Description})");
                }

                // // Execute a tool (this would normally be driven by LLM tool invocations).
                // var result = await client.CallToolAsync(
                //     "Echo",
                //     new Dictionary<string, object?>() { ["message"] = "Hello MCP!" }//,
                //     /*System.Threading.CancellationToken.None*/);

                // Console.WriteLine("MCP received the result: " + response.ToJson(true));




                //return result.ToJson(true);



                /*
                var builder = Kernel.CreateBuilder();
                //builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));


                var certs = Senparc.AI.Config.SenparcAiSetting.AzureOpenAIKeys;

                builder.Services.AddAzureOpenAIChatCompletion("gpt-4o",new AzureOpenAIClient(new Uri(certs.AzureEndpoint),new System.ClientModel.ApiKeyCredential(certs.ApiKey)));


                var kernel = builder.Build();
                await kernel.Plugins.AddMcpFunctionsFromSseServerAsync("NCF-MCP"+SystemTime.NowTicks, "http://localhost:5000/sse/sse");

                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                var prompt = request.RequestPrompt;
                var result = await kernel.InvokePromptAsync(prompt, new(executionSettings)).ConfigureAwait(false);
                Console.WriteLine($"\n\n{prompt}\n{result}");

                return result.ToJson(true);

                */

                var client2 = await McpClientFactory.CreateAsync(clientTransport);
                var tools2 = await client2.ListToolsAsync();
                // Print the list of tools available from the server.
                foreach (var tool in tools2)
                {
                    Console.WriteLine($"{tool.Name} ({tool.Description})");
                    //var kf = tool.AsKernelFunction();


                }

                var aiSetting = Senparc.AI.Config.SenparcAiSetting;
                var semanticAiHandler = new SemanticAiHandler(aiSetting);

                var parameter = new PromptConfigParameter()
                {
                    MaxTokens = 2000,
                    Temperature = 0.7,
                    TopP = 0.5,
                };

                var iWantToRun = semanticAiHandler.ChatConfig(parameter,
                  userId: "Jeffrey",
                  maxHistoryStore: 10,
                  chatSystemMessage: "你是一位智能助手，负责帮助我完成任务",
                  senparcAiSetting: aiSetting,
                  kernelBuilderAction: kh =>
                  {

                      // kh.Plugins.AddMcpFunctionsFromSseServerAsync("NCF-Server", "http://localhost:5000/sse/sse");

#pragma warning disable SKEXP0001 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.
                      kh.Plugins.AddFromFunctions("SenparcMcpPlugin", tools2.Select(z => z.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.
                  }
                      );
                var executionSettings2 = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()// FunctionChoiceBehavior.Auto()
                };
                var ka = new KernelArguments(executionSettings2) { };

                ////Output results
                //SenparcAiResult ret = await semanticAiHandler.ChatAsync(iWantToRun, request.RequestPrompt/*, streamItemProceessing*/);

                //////////var resultRaw = await iWantToRun.Kernel.InvokePromptAsync(request.RequestPrompt, ka);


                var resultRaw = await iWantToRun.Kernel.InvokePromptAsync(request.RequestPrompt, ka);
                //return resultRaw.ToJson(true);
                return resultRaw.ToString();
            });
        }



        [FunctionRender("我的函数", "我的函数的注释", typeof(Register))]
        public async Task<StringAppResponse> Calculate(MyFunction_CaculateRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                /* After clicking "Execute" on the page, the method here will be called
                  *
                  * Parameter description:
                  * response: the return result after initialization
                  * logger: log
                  * 
                  * If the properties of response are modified directly, null will eventually be returned.
                  * Otherwise, a new response object can be returned, and the system will automatically overwrite the original object.
                  */

                double calcResult = request.Number1;
                var theOperator = request.Operator.SelectedValues.FirstOrDefault();
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
                    if (request.Power.SelectedValues.Contains(power.ToString()))
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
            /* After clicking "Execute" on the page, the method here will be called
              *
              * Parameter description:
              * response: the return result after initialization
              * logger: log
              * 
              * If the properties of response are modified directly, null will eventually be returned.
              * Otherwise, a new response object can be returned, and the system will automatically overwrite the original object.
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
                        //response.ErrorMessage = "The dividend cannot be 0!";
                        return "被除数不能为0！";
                    }
                    calcResult = calcResult / request.Number2;
                    break;
                default:
                    //response.Success = false;
                    //response.ErrorMessage = $"Unknown operator: {theOperator}";
                    return $"未知的运算符：{request.TheOperator}";
            }

            //logger.Append($"Perform operation: {number1} {theOperator} {number2} = {calcResult}");

            Action<int> raisePower = p =>
            {
                if (request.Power.ToString().Contains(p.ToString()))
                {
                    if (request.Power == p)
                    {
                        var oldValue = calcResult;
                        calcResult = Math.Pow(calcResult, request.Power);
                        //logger.Append($"Perform {power} power operation: {oldValue}{(power == 2 ? "²" : "³")} = {calcResult}");
                    }
                }
            };

            raisePower(2);
            raisePower(3);

            // response.Data = $"[{theOperator}] calculation result: {calcResult}. Please see the log for the calculation process";
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
