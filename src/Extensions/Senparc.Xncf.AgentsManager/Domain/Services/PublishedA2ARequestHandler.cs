using A2A;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// 依据路由中的 agentKey 将标准 A2A 请求分发到对应的本地发布 Agent。
    /// </summary>
    public class PublishedA2ARequestHandler : IA2ARequestHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PublishedA2AServerRegistry _serverRegistry;

        public PublishedA2ARequestHandler(IHttpContextAccessor httpContextAccessor, PublishedA2AServerRegistry serverRegistry)
        {
            _httpContextAccessor = httpContextAccessor;
            _serverRegistry = serverRegistry;
        }

        public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).SendMessageAsync(request, cancellationToken);

        public async IAsyncEnumerable<StreamResponse> SendStreamingMessageAsync(
            SendMessageRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in (await ResolveServerAsync(cancellationToken)).SendStreamingMessageAsync(request, cancellationToken))
            {
                yield return item;
            }
        }

        public async Task<AgentTask> GetTaskAsync(GetTaskRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).GetTaskAsync(request, cancellationToken);

        public async Task<ListTasksResponse> ListTasksAsync(ListTasksRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).ListTasksAsync(request, cancellationToken);

        public async Task<AgentTask> CancelTaskAsync(CancelTaskRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).CancelTaskAsync(request, cancellationToken);

        public async IAsyncEnumerable<StreamResponse> SubscribeToTaskAsync(
            SubscribeToTaskRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in (await ResolveServerAsync(cancellationToken)).SubscribeToTaskAsync(request, cancellationToken))
            {
                yield return item;
            }
        }

        public async Task<TaskPushNotificationConfig> CreateTaskPushNotificationConfigAsync(CreateTaskPushNotificationConfigRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).CreateTaskPushNotificationConfigAsync(request, cancellationToken);

        public async Task<TaskPushNotificationConfig> GetTaskPushNotificationConfigAsync(GetTaskPushNotificationConfigRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).GetTaskPushNotificationConfigAsync(request, cancellationToken);

        public async Task<ListTaskPushNotificationConfigResponse> ListTaskPushNotificationConfigAsync(ListTaskPushNotificationConfigRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).ListTaskPushNotificationConfigAsync(request, cancellationToken);

        public async Task DeleteTaskPushNotificationConfigAsync(DeleteTaskPushNotificationConfigRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).DeleteTaskPushNotificationConfigAsync(request, cancellationToken);

        public async Task<AgentCard> GetExtendedAgentCardAsync(GetExtendedAgentCardRequest request, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).GetExtendedAgentCardAsync(request, cancellationToken);

        private Task<A2AServer> ResolveServerAsync(CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext
                ?? throw new A2AException("A2A request context is unavailable.", A2AErrorCode.InvalidRequest);
            var agentKey = context.Request.RouteValues["agentKey"]?.ToString();
            if (string.IsNullOrWhiteSpace(agentKey))
            {
                throw new A2AException("A2A agent key is required.", A2AErrorCode.InvalidRequest);
            }

            return _serverRegistry.GetServerAsync(agentKey, context, cancellationToken);
        }
    }

    /// <summary>
    /// 仅缓存 A2A 协议状态；每个请求仍从数据库读取发布开关和鉴权配置，因此修改无需重启。
    /// </summary>
    public class PublishedA2AServerRegistry
    {
        private readonly ConcurrentDictionary<string, A2AServer> _servers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        public PublishedA2AServerRegistry(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        public async Task<A2AServer> GetServerAsync(string rawAgentKey, HttpContext httpContext, CancellationToken cancellationToken)
        {
            string agentKey;
            try
            {
                agentKey = PublishedA2AAgent.NormalizePublicAgentKey(rawAgentKey);
            }
            catch (ArgumentException)
            {
                throw new A2AException("A2A agent is unavailable.", A2AErrorCode.InvalidRequest);
            }

            await ValidateInboundRequestAsync(agentKey, httpContext, cancellationToken);
            return _servers.GetOrAdd(agentKey, key => new A2AServer(
                new LocalPublishedA2AAgentHandler(key, _scopeFactory, _loggerFactory.CreateLogger<LocalPublishedA2AAgentHandler>()),
                new InMemoryTaskStore(),
                new ChannelEventNotifier(),
                _loggerFactory.CreateLogger<A2AServer>(),
                new A2AServerOptions { AutoAppendHistory = false }));
        }

        private async Task ValidateInboundRequestAsync(string agentKey, HttpContext httpContext, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var publishedService = scope.ServiceProvider.GetRequiredService<PublishedA2AAgentService>();
            var templateService = scope.ServiceProvider.GetRequiredService<AgentsTemplateService>();
            var publishedAgent = await publishedService.GetByPublicAgentKeyAsync(agentKey)
                ?? throw new A2AException("A2A agent is unavailable.", A2AErrorCode.InvalidRequest);
            var template = await templateService.GetAgentTemplateAsync(publishedAgent.AgentTemplateId);
            if (!publishedAgent.Enable || template == null || !template.Enable)
            {
                throw new A2AException("A2A agent is unavailable.", A2AErrorCode.InvalidRequest);
            }

            if (publishedAgent.AuthenticationMode == RemoteAgentAuthenticationMode.None)
            {
                return;
            }

            var secret = string.IsNullOrWhiteSpace(publishedAgent.AuthSecretKey)
                ? null
                : _configuration[$"A2A:InboundSecrets:{publishedAgent.AuthSecretKey}"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new A2AException("A2A authentication is not configured.", A2AErrorCode.InvalidRequest);
            }

            var supplied = publishedAgent.AuthenticationMode == RemoteAgentAuthenticationMode.BearerToken
                ? ExtractBearerToken(httpContext.Request)
                : httpContext.Request.Headers[publishedAgent.AuthHeaderName ?? string.Empty].ToString();
            if (!SecretsEqual(secret, supplied))
            {
                throw new A2AException("A2A authentication failed.", A2AErrorCode.InvalidRequest);
            }
        }

        private static string ExtractBearerToken(HttpRequest request)
        {
            var authorization = request.Headers.Authorization.ToString();
            const string prefix = "Bearer ";
            return authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? authorization[prefix.Length..].Trim()
                : string.Empty;
        }

        private static bool SecretsEqual(string expected, string supplied)
        {
            if (string.IsNullOrEmpty(supplied))
            {
                return false;
            }

            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
            return expectedBytes.Length == suppliedBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
        }
    }

    internal sealed class LocalPublishedA2AAgentHandler : IAgentHandler
    {
        private readonly string _publicAgentKey;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LocalPublishedA2AAgentHandler> _logger;

        public LocalPublishedA2AAgentHandler(
            string publicAgentKey,
            IServiceScopeFactory scopeFactory,
            ILogger<LocalPublishedA2AAgentHandler> logger)
        {
            _publicAgentKey = publicAgentKey;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
        {
            var input = context.UserText;
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new A2AException("A2A message must include a text part.", A2AErrorCode.InvalidParams);
            }

            using var scope = _scopeFactory.CreateScope();
            try
            {
                var factory = scope.ServiceProvider.GetRequiredService<PublishedA2AAgentFactory>();
                var output = await factory.RunAsync(_publicAgentKey, input, cancellationToken);
                await eventQueue.EnqueueMessageAsync(new Message
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    ContextId = context.ContextId,
                    Role = (Role)2,
                    Parts = new List<Part> { Part.FromText(output) }
                }, cancellationToken);
            }
            catch (A2AException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Published A2A agent {AgentKey} failed", _publicAgentKey);
                throw new A2AException("The A2A agent could not complete the request.", A2AErrorCode.InternalError);
            }
        }

        public async Task CancelAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
        {
            await new TaskUpdater(eventQueue, context.TaskId, context.ContextId).CancelAsync(cancellationToken);
        }
    }
}
