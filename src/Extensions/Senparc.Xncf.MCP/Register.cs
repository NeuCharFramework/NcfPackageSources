﻿using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using ModelContextProtocol.Utils.Json;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.MCP.Domain.Services;
using Senparc.Xncf.MCP.Models;
using Senparc.Xncf.MCP.Models.DatabaseModel.Dto;
using Senparc.Xncf.MCP.OHS.Local.AppService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.MCP";

        public override string Uid => "149d8021-1783-4fc9-97a8-f1a1ba60245b";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.1.0";//必须填写版本号

        public override string MenuName => "MCP Manager";

        public override string Icon => "fa fa-sliders-h";

        public override string Description => "Model Context Protocol(MCP) Manager";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //根据安装或更新不同条件执行逻辑
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //新安装
                    #region 初始化数据库数据
                    var colorService = serviceProvider.GetService<ColorAppService>();
                    var colorResult = await colorService.GetOrInitColorAsync();
                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //更新
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            MCPSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as MCPSenparcEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public IMcpClient McpClient { get; set; }

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddScoped<ColorAppService>();

            services.AddAutoMapper(z =>
            {
                z.CreateMap<Color, ColorDto>().ReverseMap();
            });

            services.AddMcpServer(opt =>
                                 {
                                     opt.ServerInfo = new Implementation()
                                     {
                                         Name = "ncf-mcp-server",
                                         Version = "1.0.0",
                                     };
                                 })
                                         //   .WithStdioServerTransport()
                                         .WithToolsFromAssembly();

            return base.AddXncfModule(services, configuration, env);
        }


        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            // var ncfMcpServerService = new McpServerService();
            // ncfMcpServerService.Start();


            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                var serviceProvider = app.ApplicationServices;
                //放置 NCF-MCP-Server SSE
                IMcpServer? server = null;
                SseResponseStreamTransport? transport = null;
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var mcpServerOptions = serviceProvider.GetRequiredService<IOptions<McpServerOptions>>();


                endpoints.MapGet("/ho", async context =>
                {
                    await context.Response.WriteAsync("Hello NCF Service!");
                });

                var routeGroup = endpoints.MapGroup("");

                endpoints.MapGet("/hoe", async context =>
                {
                    await context.Response.WriteAsync("Hello NCF Service EEE!");
                });

                routeGroup.MapGet("/ncf-mcp-sse", async (HttpContext context, HttpResponse response, CancellationToken requestAborted) =>
                {
                    // 获取配置的 Token
                    var configuredToken = Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.McpAccessToken;

                    // 从请求头获取 Token

                    if (string.IsNullOrEmpty(configuredToken))
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("MCP access token is not configured");
                        return;
                    }

                    if (!context.Request.Query.TryGetValue("token", out var requestToken) ||
                        requestToken != configuredToken)
                    {
                        Console.WriteLine($"requestToken: {requestToken}");
                        Console.WriteLine($"configuredToken: {configuredToken}");

                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Unauthorized");
                        return;
                    }

                    await using var localTransport = transport = new SseResponseStreamTransport(response.Body);
                    await using var localServer = server = McpServerFactory.Create(transport, mcpServerOptions.Value, loggerFactory, endpoints.ServiceProvider);

                    await localServer.StartAsync(requestAborted);

                    response.Headers.ContentType = "text/event-stream";
                    response.Headers.CacheControl = "no-cache";

                    try
                    {
                        await transport.RunAsync(requestAborted);
                    }
                    catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
                    {
                        // RequestAborted always triggers when the client disconnects before a complete response body is written,
                        // but this is how SSE connections are typically closed.
                    }
                });

                routeGroup.MapPost("/message", async context =>
                {
                    if (transport is null)
                    {
                        await Results.BadRequest("Connect to the /sse endpoint before sending messages.").ExecuteAsync(context);
                        return;
                    }

                    var message = await context.Request.ReadFromJsonAsync<IJsonRpcMessage>(McpJsonUtilities.DefaultOptions, context.RequestAborted);
                    if (message is null)
                    {
                        await Results.BadRequest("No message in request body.").ExecuteAsync(context);
                        return;
                    }

                    await transport.OnMessageReceivedAsync(message, context.RequestAborted);
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    await context.Response.WriteAsync("Accepted");
                });

            });

            return base.UseXncfModule(app, registerService);
        }

    }



}
