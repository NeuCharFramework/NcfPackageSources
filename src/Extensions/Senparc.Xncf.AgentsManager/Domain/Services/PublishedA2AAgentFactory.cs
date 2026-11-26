/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PublishedA2AAgentFactory.cs
    File description: domain services and business-process implementation

    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.15.0-preview20 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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
    /// Executes published Agent templates through the A2A response boundary.
    /// </summary>
    public class PublishedA2AAgentFactory
    {
        private readonly PublishedA2AAgentService _publishedA2AAgentService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly AgentTemplateRunner _agentTemplateRunner;
        private readonly ILogger<PublishedA2AAgentFactory> _logger;

        /// <summary>Initializes the published A2A factory.</summary>
        /// <param name="publishedA2AAgentService">The published-agent persistence service.</param>
        /// <param name="agentsTemplateService">The Agent-template service.</param>
        /// <param name="agentTemplateRunner">The template execution service.</param>
        /// <param name="logger">The factory logger.</param>
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

        /// <summary>Runs a published Agent with user text and returns its displayable output.</summary>
        /// <param name="publicAgentKey">The public publication key.</param>
        /// <param name="userText">The user input sent to the Agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The Agent's displayable output.</returns>
        /// <exception cref="A2AException">Thrown when publication, input, provider, or output validation fails.</exception>
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

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
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

        /// <summary>
        /// Builds a user-facing model-configuration failure message with its diagnostic identifier.
        /// </summary>
        /// <param name="providerMessage">The sanitized provider message.</param>
        /// <param name="diagnosticId">The diagnostic identifier.</param>
        /// <returns>The localized configuration failure message.</returns>
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

        /// <summary>
        /// Builds the public A2A Agent Card from publication settings and the local Agent template.
        /// </summary>
        /// <param name="publishedAgent">The publication configuration.</param>
        /// <param name="template">The local Agent template.</param>
        /// <param name="endpointUrl">The externally reachable JSON-RPC endpoint.</param>
        /// <returns>The populated Agent Card.</returns>
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

        /// <summary>
        /// Logs the effective execution profile and model diagnostics without credential content.
        /// </summary>
        /// <param name="diagnosticId">The run diagnostic identifier.</param>
        /// <param name="publishedAgent">The published Agent configuration.</param>
        /// <param name="diagnostics">The effective execution diagnostics.</param>
        private void LogExecutionModel(
            string diagnosticId,
            PublishedA2AAgent publishedAgent,
            AgentTemplateExecutionDiagnostics diagnostics)
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
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

        /// <summary>
        /// Logs a bounded summary for a failed published-A2A execution.
        /// </summary>
        /// <param name="diagnosticId">The run diagnostic identifier.</param>
        /// <param name="publishedAgent">The published Agent configuration.</param>
        /// <param name="exception">The execution exception.</param>
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

        /// <summary>
        /// Determines whether provider text contains a recognized 401/403 service failure signature.
        /// </summary>
        /// <param name="text">The provider or exception text.</param>
        /// <returns><see langword="true"/> when the text matches a supported service-failure pattern.</returns>
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

        /// <summary>
        /// Normalizes and bounds a failure string for diagnostics.
        /// </summary>
        /// <param name="text">The failure text.</param>
        /// <returns>The single-line maximum-length summary.</returns>
        private static string SummarizeFailure(string text)
        {
            var normalized = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= 240 ? normalized : normalized[..240];
        }

    }
}
