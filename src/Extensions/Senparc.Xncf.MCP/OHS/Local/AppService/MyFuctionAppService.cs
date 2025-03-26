﻿using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.MCP.Domain.Services;
using Senparc.Xncf.MCP.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Senparc.Xncf.MCP.OHS.Local.AppService
{
    public class MyFuctionAppService : AppServiceBase
    {
        private ColorService _colorService;
        private IMcpClient McpClient { get; set; }

        public MyFuctionAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            _colorService = colorService;
        }

        [FunctionRender("MCP Client", "MCP Client Test", typeof(Register))]
        public async Task<StringAppResponse> MCP()
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                try
                {
                    McpClientOptions options = new()
                    {
                        ClientInfo = new() { Name = "TestClient", Version = "1.0.0" }
                    };

                    McpClient = await McpClientFactory.CreateAsync(new()
                    {
                        Id = "everything",
                        Name = "Everything",
                        TransportType = TransportTypes.StdIo,
                        TransportOptions = new()
                        {
                            ["command"] = "npx",
                            ["arguments"] = "-y @modelcontextprotocol/server-everything",
                        }
                    }, options);


                    // Print the list of tools available from the server.
                    await foreach (var tool in McpClient.ListToolsAsync())
                    {
                        Console.WriteLine($"{tool.Name} ({tool.Description})");
                    }

                    // Execute a tool (this would normally be driven by LLM tool invocations).
                    var result = await McpClient.CallToolAsync(
                        "echo",
                        new Dictionary<string, object?>() { ["message"] = "Hello MCP!" },
                        System.Threading.CancellationToken.None);

                    // echo always returns one and only one text content object
                    return result.Content.First(c => c.Type == "text").Text;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            });
        }


        [FunctionRender("我的函数", "我的函数的注释", typeof(Register))]
        public async Task<StringAppResponse> Calculate(MyFunction_CaculateRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                /* 页面上点击“执行”后，将调用这里的方法
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
    }
}
