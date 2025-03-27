using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using ModelContextProtocol.Utils.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP.Domain.Services
{
    /// <summary>
    /// NCF 内置启动的 MCP 服务（Server）
    /// </summary>
    public class McpServerService
    {
        private IHost? _extraHost;
        private CancellationTokenSource? _cts;
        private bool _isRunning = false;

        public void Start(IServiceProvider serviceProvider)
        {
            if (_isRunning) return;  // 防止重复启动
            _cts = new CancellationTokenSource();

            _extraHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel()
                              .UseUrls("http://localhost:7000")
                              .ConfigureServices(services =>
                              {
                                  services.AddMcpServer(opt =>
                                  {
                                      opt.ServerInfo = new Implementation()
                                      {
                                          Name = "ncf-mcp-server-one",
                                          Version = "1.0.0",
                                      };
                                  })
                                        //   .WithStdioServerTransport()
                                          .WithToolsFromAssembly();
                              })
                              .Configure(app =>
                              {
                                  // 启用路由支持
                                  app.UseRouting();

                                  // 配置端点，包括 "/"
                                  app.UseEndpoints(endpoints =>
                                  {
                                      endpoints.MapGet("/", async context =>
                                      {
                                          await context.Response.WriteAsync("Hello NCF");
                                      });

                                      IMcpServer? server = null;
                                      SseResponseStreamTransport? transport = null;
                                      var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
                                      var mcpServerOptions = endpoints.ServiceProvider.GetRequiredService<IOptions<McpServerOptions>>();

                                      var routeGroup = endpoints.MapGroup("");

                                      routeGroup.MapGet("/ncf-mcp-sse", async (HttpContext context, HttpResponse response, CancellationToken requestAborted) =>
                                      {
                                          // 检查 Token
                                          var configuredToken = Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.McpAccessToken;
                                          
                                          if (string.IsNullOrEmpty(configuredToken))
                                          {
                                              context.Response.StatusCode = 500;
                                              await context.Response.WriteAsync("MCP access token is not configured");
                                              return;
                                          }

                                          if (!context.Request.Headers.TryGetValue("X-MCP-Token", out var requestToken) || 
                                              requestToken != configuredToken)
                                          {
                                              context.Response.StatusCode = 401;
                                              await context.Response.WriteAsync("Unauthorized: Invalid or missing MCP access token");
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

                              });
                }).Build();

            _isRunning = true;
            Task.Run(() => _extraHost!.RunAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (_extraHost != null && _cts != null)
            {
                _cts.Cancel();
                await _extraHost.StopAsync();
                _isRunning = false;
            }
        }

        public bool IsRunning => _isRunning;
    }
}
