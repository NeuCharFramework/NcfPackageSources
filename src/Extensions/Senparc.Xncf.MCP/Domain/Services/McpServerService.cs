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

    public class McpServerData
    {
        public string Name { get; set; }
        public IHost Host { get; set; }
        public int Port { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public bool IsRunning { get; set; }
        public string accessToken { get; set; }
    }

    /// <summary>
    /// NCF 内置启动的 MCP 服务（Server）
    /// </summary>
    public class McpServerService(IServiceProvider serviceProvider)
    {
        public static Dictionary<string, McpServerData> McpServerCollection = new Dictionary<string, McpServerData>();


        public async Task Start()
        {

            
        }


        public void Start(string mcpServerName, string version = "1.0.0")
        {
            McpServerData mcpServerData = null;

            if (McpServerCollection.ContainsKey(mcpServerName))
            {
                mcpServerData = McpServerCollection[mcpServerName];
            }
            else
            {
                mcpServerData = new McpServerData()
                {
                    Name = mcpServerName,
                    Port = 7000,
                };
                McpServerCollection[mcpServerName] = mcpServerData;
            }

            //TODO: 找一个没有被用过的端口

            IHost? _extraHost = mcpServerData.Host;
            CancellationTokenSource? _cts = mcpServerData.CancellationTokenSource;
            bool _isRunning = mcpServerData.IsRunning;

            if (_isRunning) return;  // 防止重复启动
            _cts = new CancellationTokenSource();

            _extraHost = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseKestrel()
                              .UseUrls($"http://localhost:{mcpServerData.Port}")
                              .ConfigureServices(services =>
                              {
                              services.AddMcpServer(opt =>
                              {
                                  opt.ServerInfo = new Implementation()
                                  {
                                      Name = mcpServerData.Name,
                                      Version = version,
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
                                      var configuredToken = mcpServerData.accessToken;

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

                                      await localServer.RunAsync(requestAborted);

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
                                          await Results.BadRequest("Connect to the /ncf-mcp-sse endpoint before sending messages.").ExecuteAsync(context);
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

        public async Task StopAsync(string mcpServerName)
        {
            if(McpServerCollection.ContainsKey(mcpServerName) is false)
            {
                return;
            }

            var mcpServerData = McpServerCollection[mcpServerName];
            var _extraHost = mcpServerData.Host;
            var _cts = mcpServerData.CancellationTokenSource;
            if (mcpServerData.Host != null && _cts != null)
            {
                _cts.Cancel();
                await _extraHost.StopAsync();
                mcpServerData.IsRunning = false;
            }
        }
    }
}
