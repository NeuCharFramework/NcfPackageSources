/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RemoteA2AAgentFactory.cs
    File description: domain services and business-process implementation

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using A2A;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.A2A;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// Creates remote A2A agents and validates their agent-card connectivity.
    /// </summary>
    public class RemoteA2AAgentFactory
    {
        public const string HttpClientName = "AgentsManager.A2A";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>Initializes the remote A2A factory.</summary>
        /// <param name="httpClientFactory">The named HTTP client factory.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="loggerFactory">The logger factory passed to A2A clients.</param>
        public RemoteA2AAgentFactory(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        /// <summary>Creates an AI agent backed by a remote A2A endpoint.</summary>
        /// <param name="remoteAgent">The remote endpoint configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resolved remote AI agent.</returns>
        public async Task<AIAgent> CreateAsync(RemoteAgent remoteAgent, CancellationToken cancellationToken = default)
        {
            var resolver = CreateResolver(remoteAgent, out var httpClient);
            var options = new A2AAgentOptions
            {
                Id = BuildParticipantKey(remoteAgent.Id),
                Name = remoteAgent.Name,
                Description = remoteAgent.Description
            };

            return await resolver.GetAIAgentAsync(
                options,
                httpClient,
                clientOptions: null,
                loggerFactory: _loggerFactory,
                cancellationToken: cancellationToken);
        }

        /// <summary>Loads the remote A2A Agent Card to verify connectivity.</summary>
        /// <param name="remoteAgent">The remote endpoint configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A localized success message containing the card name when available.</returns>
        public async Task<string> TestConnectionAsync(RemoteAgent remoteAgent, CancellationToken cancellationToken = default)
        {
            var resolver = CreateResolver(remoteAgent, out _);
            var card = await resolver.GetAgentCardAsync(cancellationToken);
            var cardName = card?.Name;
            return string.IsNullOrWhiteSpace(cardName)
                ? "A2A Agent Card 读取成功。"
                : $"A2A Agent Card 读取成功：{cardName}";
        }

        /// <summary>Builds the stable participant key used for a remote Agent.</summary>
        /// <param name="remoteAgentId">The persisted remote-agent identifier.</param>
        /// <returns>The stable participant key.</returns>
        public static string BuildParticipantKey(int remoteAgentId) => $"remote:{remoteAgentId}";

        /// <summary>
        /// Validates a remote Agent configuration, creates the dedicated HTTP client,
        /// applies authentication, and builds the A2A Agent Card resolver.
        /// </summary>
        /// <param name="remoteAgent">The remote Agent configuration.</param>
        /// <param name="httpClient">Receives the configured HTTP client.</param>
        /// <returns>The configured Agent Card resolver.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when protocol, URL, or authentication configuration is invalid.</exception>
        private A2ACardResolver CreateResolver(RemoteAgent remoteAgent, out HttpClient httpClient)
        {
            if (remoteAgent == null)
            {
                throw new NcfExceptionBase("远程 Agent 配置不存在。");
            }

            if (remoteAgent.Protocol != RemoteAgentProtocol.A2A)
            {
                throw new NcfExceptionBase($"暂不支持远程协议：{remoteAgent.Protocol}");
            }

            if (!Uri.TryCreate(remoteAgent.AgentCardUrl, UriKind.Absolute, out var configuredUri)
                || (configuredUri.Scheme != Uri.UriSchemeHttp && configuredUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new NcfExceptionBase("远程 Agent 的 A2A 地址必须是有效的 HTTP 或 HTTPS 地址。");
            }

            httpClient = _httpClientFactory.CreateClient(HttpClientName);
            httpClient.Timeout = TimeSpan.FromSeconds(remoteAgent.TimeoutSeconds <= 0 ? 60 : remoteAgent.TimeoutSeconds);
            ApplyAuthentication(remoteAgent, httpClient);

            var (baseUrl, agentCardPath) = SplitAgentCardUrl(configuredUri);
            return new A2ACardResolver(baseUrl, httpClient, agentCardPath, logger: null);
        }

        /// <summary>
        /// Applies bearer-token or custom-header authentication from the configured secret store.
        /// </summary>
        /// <param name="remoteAgent">The remote Agent authentication settings.</param>
        /// <param name="httpClient">The HTTP client receiving authentication headers.</param>
        /// <exception cref="NcfExceptionBase">Thrown when required secret or header configuration is missing.</exception>
        private void ApplyAuthentication(RemoteAgent remoteAgent, HttpClient httpClient)
        {
            if (remoteAgent.AuthenticationMode == RemoteAgentAuthenticationMode.None)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(remoteAgent.AuthSecretKey))
            {
                throw new NcfExceptionBase("远程 Agent 已启用鉴权，但未设置部署配置密钥名。");
            }

            var secret = _configuration[$"A2A:Secrets:{remoteAgent.AuthSecretKey}"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new NcfExceptionBase($"未找到远程 Agent 鉴权密钥配置：A2A:Secrets:{remoteAgent.AuthSecretKey}");
            }

            if (remoteAgent.AuthenticationMode == RemoteAgentAuthenticationMode.BearerToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secret);
                return;
            }

            if (string.IsNullOrWhiteSpace(remoteAgent.AuthHeaderName))
            {
                throw new NcfExceptionBase("CustomHeader 鉴权必须设置请求头名称。");
            }

            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(remoteAgent.AuthHeaderName, secret);
        }

        /// <summary>
        /// Splits a configured Agent Card URL into the resolver base URL and relative card path.
        /// </summary>
        /// <param name="configuredUri">The validated configured Agent Card URI.</param>
        /// <returns>The base URI and relative Agent Card path.</returns>
        private static (Uri BaseUrl, string AgentCardPath) SplitAgentCardUrl(Uri configuredUri)
        {
            const string wellKnownPath = "/.well-known/";
            var absolutePath = configuredUri.AbsolutePath;
            var wellKnownIndex = absolutePath.IndexOf(wellKnownPath, StringComparison.OrdinalIgnoreCase);
            if (wellKnownIndex < 0)
            {
                var rootUrl = configuredUri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/";
                return (new Uri(rootUrl, UriKind.Absolute), ".well-known/agent-card.json");
            }

            var basePath = absolutePath.Substring(0, wellKnownIndex);
            var baseUrlText = configuredUri.GetLeftPart(UriPartial.Authority) + basePath;
            if (!baseUrlText.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrlText += "/";
            }

            // A2ACardResolver combines baseUrl and agentCardPath as a relative URI.
            // Passing the complete configured path here duplicates the path segment for
            // per-agent discovery URLs such as /a2a/{key}/.well-known/agent-card.json.
            var agentCardPath = absolutePath.Substring(wellKnownIndex).TrimStart('/');
            if (!string.IsNullOrWhiteSpace(configuredUri.Query))
            {
                agentCardPath += configuredUri.Query;
            }

            return (new Uri(baseUrlText, UriKind.Absolute), agentCardPath);
        }
    }
}
