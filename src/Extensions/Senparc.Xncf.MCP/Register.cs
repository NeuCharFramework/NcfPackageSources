/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Register.cs
    File description: module registration and initialization logic
    
    
    Created by: Senparc - 20200818
    
    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260717
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261004
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.5.10 use compatible icon for MCP module

    Modified by: Senparc - 20261111
    Change description: v0.5.13 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.MCP.Domain.Services;
using Senparc.Xncf.MCP.Models;
using Senparc.Xncf.MCP.OHS.Local.AppService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using ModelContextProtocol.Protocol;

namespace Senparc.Xncf.MCP
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<McpResource>(
            "Module.MCP",
            "MCP 管理器",
            "Model Context Protocol（MCP）管理器");

        #region Legacy section

        public override string Name => "Senparc.Xncf.MCP";

        public override string Uid => "149d8021-1783-4fc9-97a8-f1a1ba60245b";//Legacy comment translated to English.

        public override string Version => "0.5.8";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-plug";

        public override string Description => RegisterText.Description;

        public override bool EnableMcpServer => true;

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //update the database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //execute logic according to the installation or update condition
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //new installation
                    break;
                case InstallOrUpdate.Update:
                    //update
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            MCPSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as MCPSenparcEntities;

            //specify the data entities to delete

            //Legacy comment translated to English.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        //public IMcpClient McpClient { get; set; }

        public List<string> McpFunctionMetaInfo { get; set; }

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<MCPEndpointAppService>();
            services.AddScoped<MCPEndpointService>();
            services.AddScoped<McpConnectionTestService>();
            services.AddScoped<PublishedMcpServerService>(provider => new PublishedMcpServerService(
                XncfRegisterManager.McpServerInfoCollection, provider.GetRequiredService<EndpointDataSource>()));

            return base.AddXncfModule(services, configuration, env);
        }


        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            // var ncfMcpServerService = new McpServerService();
            // ncfMcpServerService.Start();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });

            if (app is IEndpointRouteBuilder endpoints)
            {
                //Legacy comment translated to English.
                // var routePattern = "mcp";
                // endpoints.MapMcp(routePattern);

                ///Legacy comment translated to English.
                //var routeGroup = endpoints.MapGroup(routePattern);
                //var routeEndpoints  = endpoints.DataSources.SelectMany(z=>z.Endpoints)
                //    .OfType<RouteEndpoint>()
                //    .Where(z=>z.RoutePattern.RawText == routePattern,)
            }

            //app.UseRouting();
            //app.UseAuthorization();
            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapMcp("", async (context, options, ct) =>
            //    {
            //        var configuredToken = Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.McpAccessToken;

            //Legacy comment translated to English.

            //        //if (string.IsNullOrEmpty(configuredToken))
            //        //{
            //        //    context.Response.StatusCode = 500;
            //        //    await context.Response.WriteAsync("MCP access token is not configured");
            //        //    return;
            //        //}

            //        //if (!context.Request.Query.TryGetValue("token", out var requestToken) ||
            //        //    requestToken != configuredToken)
            //        //{
            //        //    Console.WriteLine($"requestToken: {requestToken}");
            //        //    Console.WriteLine($"configuredToken: {configuredToken}");

            //        //    context.Response.StatusCode = 401;
            //        //    await context.Response.WriteAsync("Unauthorized");
            //        //    return;
            //        //}
            //    }, async (context, mcpServer, ct) =>
            //    {
            //    }
            //    );
            //});


            //app.UseEndpoints(endpoints =>
            //{
            //    var serviceProvider = app.ApplicationServices;
            //Legacy comment translated to English.
            //    IMcpServer? server = null;
            //    SseResponseStreamTransport? transport = null;
            //    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            //    var mcpServerOptions = serviceProvider.GetRequiredService<IOptions<McpServerOptions>>();

            //    var routeGroup = endpoints.MapGroup("");
            //    routeGroup.MapGet("/ncf-mcp-sse", async (HttpContext context, HttpResponse response, CancellationToken requestAborted) =>
            //    {
            //Legacy comment translated to English.
            //        var configuredToken = Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.McpAccessToken;

            //Legacy comment translated to English.

            //        if (string.IsNullOrEmpty(configuredToken))
            //        {
            //            context.Response.StatusCode = 500;
            //            await context.Response.WriteAsync("MCP access token is not configured");
            //            return;
            //        }

            //        if (!context.Request.Query.TryGetValue("token", out var requestToken) ||
            //            requestToken != configuredToken)
            //        {
            //            Console.WriteLine($"requestToken: {requestToken}");
            //            Console.WriteLine($"configuredToken: {configuredToken}");

            //            context.Response.StatusCode = 401;
            //            await context.Response.WriteAsync("Unauthorized");
            //            return;
            //        }

            //        await using var localTransport = transport = new SseResponseStreamTransport(response.Body);
            //        await using var localServer = server = McpServerFactory.Create(transport, mcpServerOptions.Value, loggerFactory, endpoints.ServiceProvider);

            //        await localServer.RunAsync(requestAborted);

            //        response.Headers.ContentType = "text/event-stream";
            //        response.Headers.CacheControl = "no-cache";

            //        try
            //        {
            //            await transport.RunAsync(requestAborted);
            //        }
            //        catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
            //        {
            //            // RequestAborted always triggers when the client disconnects before a complete response body is written,
            //            // but this is how SSE connections are typically closed.
            //        }
            //    });

            //    routeGroup.MapPost("/message", async context =>
            //    {
            //        if (transport is null)
            //        {
            //            await Results.BadRequest("Connect to the /ncf-mcp-sse endpoint before sending messages.").ExecuteAsync(context);
            //            return;
            //        }

            //        var message = await context.Request.ReadFromJsonAsync<IJsonRpcMessage>(McpJsonUtilities.DefaultOptions, context.RequestAborted);
            //        if (message is null)
            //        {
            //            await Results.BadRequest("No message in request body.").ExecuteAsync(context);
            //            return;
            //        }

            //        await transport.OnMessageReceivedAsync(message, context.RequestAborted);
            //        context.Response.StatusCode = StatusCodes.Status202Accepted;
            //        await context.Response.WriteAsync("Accepted");
            //    });

            //});



            return base.UseXncfModule(app, registerService);
        }

    }



}
