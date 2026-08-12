using A2A;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// A2A 发布适配器。协议请求经鉴权后复用 <see cref="AgentTemplateRunner"/> 执行本地 AgentTemplate。
    /// 不把 Prompt 或工具描述写入 Agent Card。
    /// </summary>
    public class PublishedA2AAgentFactory
    {
        private readonly PublishedA2AAgentService _publishedA2AAgentService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly AgentTemplateRunner _agentTemplateRunner;
        private readonly ILogger<PublishedA2AAgentFactory> _logger;

        public PublishedA2AAgentFactory(
            PublishedA2AAgentService publishedA2AAgentService,
            AgentsTemplateService agentsTemplateService,
            AgentTemplateRunner agentTemplateRunner,
            ILogger<PublishedA2AAgentFactory> logger)
        {
            _publishedA2AAgentService = publishedA2AAgentService;
            _agentsTemplateService = agentsTemplateService;
            _agentTemplateRunner = agentTemplateRunner;
            _logger = logger;
        }

        public async Task<string> RunAsync(string publicAgentKey, string userText, CancellationToken cancellationToken = default)
        {
            var (publishedAgent, template) = await GetActiveAgentAsync(publicAgentKey);
            var diagnosticId = Guid.NewGuid().ToString("N")[..12];
            userText ??= string.Empty;
            if (userText.Length > publishedAgent.MaxInputCharacters)
            {
                throw new A2AException(
                    $"A2A message exceeds the configured maximum of {publishedAgent.MaxInputCharacters} characters.",
                    A2AErrorCode.InvalidParams);
            }

            var execution = await _agentTemplateRunner.RunAsync(
                template,
                userText,
                AgentTemplateRunRequest.ForPublishedA2A(
                    template.Id,
                    publishedAgent.PublicAgentKey,
                    publishedAgent.AllowFunctionCalls),
                diagnostics => LogExecutionModel(diagnosticId, publishedAgent, diagnostics),
                cancellationToken).ConfigureAwait(false);
            if (!execution.Success)
            {
                throw new A2AException(execution.ErrorMessage ?? "The A2A agent did not return a displayable response.", A2AErrorCode.InvalidAgentResponse);
            }

            var output = execution.Output;

            // Several provider adapters encode an upstream HTTP failure as OutputString instead
            // of throwing. Do not publish it as a successful A2A message; otherwise a mixed
            // ChatGroup treats the failure text as an Agent conclusion and broadcasts it.
            if (ContainsServiceFailureSignature(output))
            {
                _logger.LogWarning(
                    "Published A2A run {DiagnosticId} for agent {AgentKey} received an upstream model-service failure. {ModelDescription} Failure: {Failure}",
                    diagnosticId,
                    publishedAgent.PublicAgentKey,
                    execution.Diagnostics.ModelDescription,
                    SummarizeFailure(output));
                SenparcTrace.SendCustomLog(
                    "AgentsManager.A2A.UpstreamModelFailure",
                    $"DiagnosticId={diagnosticId}; Agent={publishedAgent.PublicAgentKey}; " +
                    execution.Diagnostics.ModelDescription + "; Failure=" + SummarizeFailure(output));
                throw new A2AException(
                    $"The published A2A agent's upstream model service rejected the request. DiagnosticId: {diagnosticId}. Check server diagnostics.",
                    A2AErrorCode.InternalError);
            }

            return output;
        }

        public async Task<(PublishedA2AAgent PublishedAgent, AgentTemplate Template)> GetActiveAgentAsync(string publicAgentKey)
        {
            var publishedAgent = await _publishedA2AAgentService.GetByPublicAgentKeyAsync(publicAgentKey)
                ?? throw new A2AException("A2A agent is unavailable.", A2AErrorCode.InvalidRequest);
            var template = await _agentsTemplateService.GetAgentTemplateAsync(publishedAgent.AgentTemplateId);
            if (!publishedAgent.Enable || template == null || !template.Enable)
            {
                throw new A2AException("A2A agent is unavailable.", A2AErrorCode.InvalidRequest);
            }

            return (publishedAgent, template);
        }

        public AgentCard BuildAgentCard(PublishedA2AAgent publishedAgent, AgentTemplate template, string endpointUrl)
        {
            var cardName = string.IsNullOrWhiteSpace(publishedAgent.CardName) ? template.Name : publishedAgent.CardName;
            var cardDescription = string.IsNullOrWhiteSpace(publishedAgent.CardDescription)
                ? (string.IsNullOrWhiteSpace(template.Description) ? "A local NCF agent published through A2A." : template.Description)
                : publishedAgent.CardDescription;
            var skillName = string.IsNullOrWhiteSpace(publishedAgent.SkillName) ? cardName : publishedAgent.SkillName;
            var skillDescription = string.IsNullOrWhiteSpace(publishedAgent.SkillDescription) ? cardDescription : publishedAgent.SkillDescription;

            return new AgentCard
            {
                Name = cardName,
                Description = cardDescription,
                Version = "1.0",
                SupportedInterfaces = new List<AgentInterface>
                {
                    new()
                    {
                        Url = endpointUrl,
                        ProtocolBinding = "JSONRPC",
                        ProtocolVersion = "1.0"
                    }
                },
                Capabilities = new AgentCapabilities
                {
                    Streaming = false,
                    PushNotifications = false,
                    ExtendedAgentCard = false
                },
                Skills = new List<A2A.AgentSkill>
                {
                    new()
                    {
                        Id = string.IsNullOrWhiteSpace(publishedAgent.SkillId) ? "chat" : publishedAgent.SkillId,
                        Name = skillName,
                        Description = skillDescription,
                        Tags = new List<string> { "a2a", "ncf", "chat" },
                        InputModes = new List<string> { "text/plain" },
                        OutputModes = new List<string> { "text/plain" }
                    }
                },
                DefaultInputModes = new List<string> { "text/plain" },
                DefaultOutputModes = new List<string> { "text/plain" }
            };
        }

        private void LogExecutionModel(
            string diagnosticId,
            PublishedA2AAgent publishedAgent,
            AgentTemplateExecutionDiagnostics diagnostics)
        {
            // Diagnostics deliberately contain only model metadata and counts. Prompt、密钥、完整端点、工具定义
            // 都不会写入日志，便于安全地将 A2A 路径与本地 Agent 执行配置比对。
            _logger.LogInformation(
                "Published A2A run {DiagnosticId} for agent {AgentKey} (template {TemplateId}) uses {ExecutionProfile}; {ModelDescription}; credential={CredentialState}; session={SessionStrategy}; functions={FunctionCallsEnabled}; toolCount={ToolCount}",
                diagnosticId,
                publishedAgent.PublicAgentKey,
                diagnostics.TemplateId,
                diagnostics.ExecutionProfile,
                diagnostics.ModelDescription,
                diagnostics.CredentialState,
                diagnostics.SessionStrategy,
                diagnostics.FunctionCallsEnabled,
                diagnostics.ToolCount);
            SenparcTrace.SendCustomLog(
                "AgentsManager.A2A.ExecutionModel",
                $"DiagnosticId={diagnosticId}; Agent={publishedAgent.PublicAgentKey}; TemplateId={diagnostics.TemplateId}; " +
                $"profile={diagnostics.ExecutionProfile}; {diagnostics.ModelDescription}; " +
                $"credential={diagnostics.CredentialState}; session={diagnostics.SessionStrategy}; " +
                $"functionCalls={diagnostics.FunctionCallsEnabled}; toolCount={diagnostics.ToolCount}");
        }

        private static bool ContainsServiceFailureSignature(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var normalized = text.Trim();
            var has403 = normalized.Contains("Status: 403", StringComparison.OrdinalIgnoreCase)
                         || normalized.Contains("Status:403", StringComparison.OrdinalIgnoreCase)
                         || normalized.Contains("StatusCode: 403", StringComparison.OrdinalIgnoreCase)
                         || normalized.Contains("StatusCode:403", StringComparison.OrdinalIgnoreCase);
            var has401 = normalized.Contains("Status: 401", StringComparison.OrdinalIgnoreCase)
                         || normalized.Contains("Status:401", StringComparison.OrdinalIgnoreCase)
                         || normalized.Contains("StatusCode: 401", StringComparison.OrdinalIgnoreCase)
                         || normalized.Contains("StatusCode:401", StringComparison.OrdinalIgnoreCase);
            var hasServiceFailed = normalized.Contains("Service request failed", StringComparison.OrdinalIgnoreCase)
                                   || normalized.Contains("ClientResultException", StringComparison.OrdinalIgnoreCase);

            return hasServiceFailed && (has403 || has401);
        }

        private static string SummarizeFailure(string text)
        {
            var normalized = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= 240 ? normalized : normalized[..240];
        }

    }
}
