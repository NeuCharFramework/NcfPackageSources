/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptInitResponseHandler.cs
    File description: PromptInitResponseHandler related implementation

    Created by: Senparc - 20260306

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.Application.EventHandlers
{
    public class PromptInitResponseHandler : IIntegrationEventHandler<PromptInitResponseEvent>
    {
        private readonly PromptOptimizationService _optimizationService;
        private readonly ILogger<PromptInitResponseHandler> _logger;

        /// <summary>
        /// Initializes the PromptRange initialization-response handler.
        /// </summary>
        /// <param name="optimizationService">The service completing pending initialization requests.</param>
        /// <param name="logger">The event-handler logger.</param>
        public PromptInitResponseHandler(
            PromptOptimizationService optimizationService,
            ILogger<PromptInitResponseHandler> logger)
        {
            _optimizationService = optimizationService;
            _logger = logger;
        }

        /// <summary>
        /// Completes the pending PromptRange initialization request represented by the event.
        /// </summary>
        /// <param name="event">The initialization response event.</param>
        /// <param name="cancellationToken">The handler cancellation token.</param>
        /// <returns>A completed task after the pending request is completed.</returns>
        public Task Handle(PromptInitResponseEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Prompt Init Response: {@event.PromptCode}, RequestId: {@event.RequestId}");
            _optimizationService.CompleteInitRequest(@event.RequestId, @event);
            return Task.CompletedTask;
        }
    }
}
