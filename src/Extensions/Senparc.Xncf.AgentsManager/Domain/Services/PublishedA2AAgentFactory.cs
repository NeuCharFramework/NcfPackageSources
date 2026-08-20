/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PublishedA2AAgentFactory.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

    修改标识：Senparc - 20260815
    修改描述：v0.15.0-preview20 增强 AgentTemplate、ChatGroup 与发布型 A2A 的取消和请求处理

----------------------------------------------------------------*/

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

            // 发布型 A2A 只负责协议与授权边界；模板解析、Prompt 参数、模型配置、
            // AgentKernel 构建和响应执行全部复用本地独立 Agent 的 RunAsync 入口。
            // 不在正常路径替换 HttpClient、API Version、Deployment 或模型。
            AgentTemplateRunResult execution;
            try
            {
                execution = await _agentTemplateRunner.RunAsync(
                    template,
                    userText,
                    AgentTemplateRunRequest.ForPublishedA2A(
                        template.Id,
                        publishedAgent.PublicAgentKey,
                        publishedAgent.AllowFunctionCalls,
                        diagnosticId),
                    diagnostics => LogExecutionModel(diagnosticId, publishedAgent, diagnostics),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (PublishedA2AModelProviderException ex)
            {
                LogExecutionFailure(diagnosticId, publishedAgent, ex);
                throw new A2AException(
                    BuildProviderConfigurationFailureMessage(ex.Message, diagnosticId),
                    A2AErrorCode.InternalError);
            }
            catch (Exception ex)
            {
                LogExecutionFailure(diagnosticId, publishedAgent, ex);
                throw new A2AException(
                    $"The published A2A agent failed while processing the model response. DiagnosticId: {diagnosticId}. Check server diagnostics.",
                    A2AErrorCode.InternalError);
            }
            finally
            {
                PublishedA2AModelTransport.ClearFailure(diagnosticId);
            }
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

        private static string BuildProviderConfigurationFailureMessage(string providerMessage, string diagnosticId)
        {
            return $"远程 A2A Agent 的模型配置当前不可用：{providerMessage} DiagnosticId: {diagnosticId}。";
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
                "Published A2A run {DiagnosticId} for agent {AgentKey} (template {TemplateId}) uses {ExecutionProfile}; {ModelDescription}; credential={CredentialState}; session={SessionStrategy}; modelTransport=local-standard; modelRequest=strict-non-streaming; {ExecutionParameters}; functions={FunctionCallsEnabled}; toolCount={ToolCount}",
                diagnosticId,
                publishedAgent.PublicAgentKey,
                diagnostics.TemplateId,
                diagnostics.ExecutionProfile,
                diagnostics.ModelDescription,
                diagnostics.CredentialState,
                diagnostics.SessionStrategy,
                diagnostics.ExecutionParameters,
                diagnostics.FunctionCallsEnabled,
                diagnostics.ToolCount);
            SenparcTrace.SendCustomLog(
                "AgentsManager.A2A.ExecutionModel",
                $"DiagnosticId={diagnosticId}; Agent={publishedAgent.PublicAgentKey}; TemplateId={diagnostics.TemplateId}; " +
                $"profile={diagnostics.ExecutionProfile}; {diagnostics.ModelDescription}; " +
                $"credential={diagnostics.CredentialState}; session={diagnostics.SessionStrategy}; modelTransport=local-standard; " +
                $"modelRequest=strict-non-streaming; {diagnostics.ExecutionParameters}; " +
                $"functionCalls={diagnostics.FunctionCallsEnabled}; toolCount={diagnostics.ToolCount}");
        }

        private void LogExecutionFailure(string diagnosticId, PublishedA2AAgent publishedAgent, Exception exception)
        {
            var summary = SummarizeFailure($"{exception.GetType().Name}: {exception.Message}");
            _logger.LogError(
                exception,
                "Published A2A run {DiagnosticId} for agent {AgentKey} failed while aggregating the model response. {Failure}",
                diagnosticId,
                publishedAgent.PublicAgentKey,
                summary);
            SenparcTrace.SendCustomLog(
                "AgentsManager.A2A.ExecutionFailure",
                $"DiagnosticId={diagnosticId}; Agent={publishedAgent.PublicAgentKey}; " +
                $"ExceptionType={exception.GetType().FullName}; Failure={summary}");
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
