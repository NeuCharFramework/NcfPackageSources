/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PublishedA2ARequestHandler.cs
    File description: domain services and business-process implementation

    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261028
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.19.3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using A2A;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.MultiTenant;
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
    /// Routes A2A requests to the published server selected by the current request's public agent key.
    /// </summary>
    public class PublishedA2ARequestHandler : IA2ARequestHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PublishedA2AServerRegistry _serverRegistry;

        /// <summary>
        /// Initializes a request handler with the current HTTP context and published-server registry.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor used to read the current route and request context.</param>
        /// <param name="serverRegistry">Registry used to resolve and cache the published A2A server.</param>
        public PublishedA2ARequestHandler(IHttpContextAccessor httpContextAccessor, PublishedA2AServerRegistry serverRegistry)
        {
            _httpContextAccessor = httpContextAccessor;
            _serverRegistry = serverRegistry;
        }

        /// <inheritdoc />
        public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).SendMessageAsync(request, context, cancellationToken);

        /// <inheritdoc />
        public async IAsyncEnumerable<StreamResponse> SendStreamingMessageAsync(
            SendMessageRequest request,
            A2ARequestContext? context,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in (await ResolveServerAsync(cancellationToken)).SendStreamingMessageAsync(request, context, cancellationToken))
            {
                yield return item;
            }
        }

        /// <inheritdoc />
        public async Task<AgentTask> GetTaskAsync(GetTaskRequest request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).GetTaskAsync(request, context, cancellationToken);

        /// <inheritdoc />
        public async Task<ListTasksResponse> ListTasksAsync(ListTasksRequest request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).ListTasksAsync(request, context, cancellationToken);

        /// <inheritdoc />
        public async Task<AgentTask> CancelTaskAsync(CancelTaskRequest request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).CancelTaskAsync(request, context, cancellationToken);

        /// <inheritdoc />
        public async IAsyncEnumerable<StreamResponse> SubscribeToTaskAsync(
            SubscribeToTaskRequest request,
            A2ARequestContext? context,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in (await ResolveServerAsync(cancellationToken)).SubscribeToTaskAsync(request, context, cancellationToken))
            {
                yield return item;
            }
        }

        /// <inheritdoc />
        public async Task<TaskPushNotificationConfig> CreateTaskPushNotificationConfigAsync(TaskPushNotificationConfig request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).CreateTaskPushNotificationConfigAsync(request, context, cancellationToken);

        /// <inheritdoc />
        public async Task<TaskPushNotificationConfig> GetTaskPushNotificationConfigAsync(GetTaskPushNotificationConfigRequest request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).GetTaskPushNotificationConfigAsync(request, context, cancellationToken);

        /// <inheritdoc />
        public async Task<ListTaskPushNotificationConfigsResponse> ListTaskPushNotificationConfigsAsync(ListTaskPushNotificationConfigsRequest request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).ListTaskPushNotificationConfigsAsync(request, context, cancellationToken);

        /// <inheritdoc />
        public async Task DeleteTaskPushNotificationConfigAsync(DeleteTaskPushNotificationConfigRequest request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).DeleteTaskPushNotificationConfigAsync(request, context, cancellationToken);

        /// <inheritdoc />
        public async Task<AgentCard> GetExtendedAgentCardAsync(GetExtendedAgentCardRequest request, A2ARequestContext? context, CancellationToken cancellationToken)
            => await (await ResolveServerAsync(cancellationToken)).GetExtendedAgentCardAsync(request, context, cancellationToken);

        /// <summary>
        /// Resolves the published A2A server for the current route's <c>agentKey</c>.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel server resolution.</param>
        /// <returns>A task that completes with the resolved A2A server.</returns>
        /// <exception cref="A2AException">Thrown when the HTTP context or route agent key is unavailable.</exception>
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
    /// Resolves and caches tenant-aware A2A servers for published agent keys.
    /// It validates publication state and inbound authentication before creating a server.
    /// </summary>
    public class PublishedA2AServerRegistry
    {
        private readonly ConcurrentDictionary<string, A2AServer> _servers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes the published-server registry.
        /// </summary>
        /// <param name="scopeFactory">Factory used to create tenant-aware validation and execution scopes.</param>
        /// <param name="configuration">Configuration containing inbound A2A secrets.</param>
        /// <param name="loggerFactory">Factory used to create server and handler loggers.</param>
        public PublishedA2AServerRegistry(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Validates the requested public agent key and returns a cached server for the current tenant.
        /// </summary>
        /// <param name="rawAgentKey">The unnormalized public agent key from the route.</param>
        /// <param name="httpContext">The current HTTP request context used for tenant and authentication checks.</param>
        /// <param name="cancellationToken">Token used to cancel validation or server creation.</param>
        /// <returns>A task that completes with the tenant-scoped A2A server.</returns>
        /// <exception cref="A2AException">Thrown when the agent is invalid, disabled, unpublished, or authentication fails.</exception>
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

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var tenantSnapshot = RequestTenantSnapshot.Capture(httpContext?.RequestServices);
            await ValidateInboundRequestAsync(agentKey, httpContext, tenantSnapshot, cancellationToken);
            var serverCacheKey = SiteConfig.SenparcCoreSetting.EnableMultiTenant
                ? $"{tenantSnapshot.Id}:{agentKey}"
                : agentKey;
            return _servers.GetOrAdd(serverCacheKey, _ => new A2AServer(
                new LocalPublishedA2AAgentHandler(agentKey, _scopeFactory, tenantSnapshot, _loggerFactory.CreateLogger<LocalPublishedA2AAgentHandler>()),
                new InMemoryTaskStore(),
                new ChannelEventNotifier(),
                _loggerFactory.CreateLogger<A2AServer>(),
                new A2AServerOptions { AutoAppendHistory = false }));
        }

        /// <summary>
        /// Validates publication state, template availability, and configured inbound authentication.
        /// </summary>
        /// <param name="agentKey">The normalized public agent key.</param>
        /// <param name="httpContext">The current HTTP request context.</param>
        /// <param name="tenantSnapshot">The tenant context restored for validation.</param>
        /// <param name="cancellationToken">Token used to cancel database lookups.</param>
        /// <exception cref="A2AException">Thrown when the published agent is unavailable or authentication fails.</exception>
        private async Task ValidateInboundRequestAsync(string agentKey, HttpContext httpContext, RequestTenantSnapshot tenantSnapshot, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            tenantSnapshot.Restore(scope.ServiceProvider);
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

        /// <summary>
        /// Extracts a bearer token from the HTTP Authorization header.
        /// </summary>
        /// <param name="request">The request whose Authorization header should be inspected.</param>
        /// <returns>The trimmed bearer token, or an empty string when the header is not a bearer header.</returns>
        private static string ExtractBearerToken(HttpRequest request)
        {
            var authorization = request.Headers.Authorization.ToString();
            const string prefix = "Bearer ";
            return authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? authorization[prefix.Length..].Trim()
                : string.Empty;
        }

        /// <summary>
        /// Compares two secret values in constant time after UTF-8 encoding.
        /// </summary>
        /// <param name="expected">The configured secret.</param>
        /// <param name="supplied">The request-supplied secret.</param>
        /// <returns><see langword="true"/> when both non-empty secrets have equal byte sequences.</returns>
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
        private readonly RequestTenantSnapshot _tenantSnapshot;
        private readonly ILogger<LocalPublishedA2AAgentHandler> _logger;

        /// <summary>
        /// Initializes a local published-agent handler.
        /// </summary>
        /// <param name="publicAgentKey">The normalized public agent key to execute.</param>
        /// <param name="scopeFactory">Factory used to create the tenant-aware execution scope.</param>
        /// <param name="tenantSnapshot">Tenant context restored before running the agent.</param>
        /// <param name="logger">Logger used to record unexpected execution failures.</param>
        public LocalPublishedA2AAgentHandler(
            string publicAgentKey,
            IServiceScopeFactory scopeFactory,
            RequestTenantSnapshot tenantSnapshot,
            ILogger<LocalPublishedA2AAgentHandler> logger)
        {
            _publicAgentKey = publicAgentKey;
            _scopeFactory = scopeFactory;
            _tenantSnapshot = tenantSnapshot;
            _logger = logger;
        }

        /// <summary>
        /// Executes one inbound A2A message through the published agent and enqueues its response.
        /// </summary>
        /// <param name="context">The A2A request context and user text.</param>
        /// <param name="eventQueue">The event queue receiving the generated response.</param>
        /// <param name="cancellationToken">Token used to cancel execution or event delivery.</param>
        /// <exception cref="A2AException">Thrown when the message has no text or execution fails.</exception>
        public async Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
        {
            var input = context.UserText;
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new A2AException("A2A message must include a text part.", A2AErrorCode.InvalidParams);
            }

            using var scope = _scopeFactory.CreateScope();
            _tenantSnapshot.Restore(scope.ServiceProvider);
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

        /// <summary>
        /// Publishes the cancellation state for the current A2A task.
        /// </summary>
        /// <param name="context">The A2A request context containing the task identity.</param>
        /// <param name="eventQueue">The event queue receiving the cancellation update.</param>
        /// <param name="cancellationToken">Token used to cancel event delivery.</param>
        public async Task CancelAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
        {
            await new TaskUpdater(eventQueue, context.TaskId, context.ContextId).CancelAsync(null, cancellationToken);
        }
    }
}
