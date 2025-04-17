using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Server;
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


    [McpServerToolType()]
    public static class NcfMcpTools
    {
        [McpServerTool, Description("Echoes the message back to the client.")]
        public static string Echo(string message)
        {
            Console.WriteLine("Echo 收到来自 MCP的 请求，Message:" + message);
            return $"hello {message}";
        }

        [McpServerTool, Description("return current time.")]
        public static string Now(string message) { return $"{DateTime.Now}"; }

        //自动增加小时数
        [McpServerTool, Description("自动增加小时数")]
        public static string AddHours(int hours)
        {
            return $"{DateTime.Now.AddHours(hours)}";
        }


    }

    [McpServerToolType]
    public class MyFuctionAppServiceForCalc
    {

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

        [FunctionRender("获取当前时间", "获取当前时间", typeof(Register))]
        public async Task<StringAppResponse> GetNoew()
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

                var clientTransport = new SseClientTransport(new SseClientTransportOptions() { 
                 Endpoint= new Uri( "http://localhost:5000/sse/sse"),
                  Name= "NCF-Server"

                });

                var client = await McpClientFactory.CreateAsync(clientTransport);

                // Print the list of tools available from the server.
                foreach (var tool in await client.ListToolsAsync())
                {
                    Console.WriteLine($"{tool.Name} ({tool.Description})");
                }

                // Execute a tool (this would normally be driven by LLM tool invocations).
                var result = await client.CallToolAsync(
                    "Echo",
                    new Dictionary<string, object?>() { ["message"] = "Hello MCP!" }//,
                    /*System.Threading.CancellationToken.None*/);

                Console.WriteLine("MCP 收到结果：" + response.ToJson(true));

                return result.ToJson(true);
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
            //int number1, int number2, int power, [Description("Number1 和 Number2 之间的运算符，可选：+ - × ÷")] string operatorMark
            RequestType request
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
            //RequestType request = new RequestType
            //{
            //    Power = power,
            //    Number1 = number1,
            //    Number2 = number2,
            //    TheOperator = operatorMark
            //};

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
