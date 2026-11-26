/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptOptimizationResponseHandler.cs
    File description: PromptOptimizationResponseHandler related implementation

    Created by: Senparc - 20260306

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Senparc.Xncf.AgentsManager.Application.EventHandlers
{
    public class PromptOptimizationResponseHandler : IIntegrationEventHandler<PromptOptimizationResponseEvent>
    {
        private readonly PromptOptimizationService _optimizationService;
        private readonly ILogger<PromptOptimizationResponseHandler> _logger;

        /// <summary>
        /// Initializes the PromptOptimization response handler.
        /// </summary>
        /// <param name="optimizationService">The service completing pending optimization requests.</param>
        /// <param name="logger">The event-handler logger.</param>
        public PromptOptimizationResponseHandler(
            PromptOptimizationService optimizationService,
            ILogger<PromptOptimizationResponseHandler> logger)
        {
            _optimizationService = optimizationService;
            _logger= logger;
        }

        /// <summary>
        /// Completes the pending optimization request represented by the response event.
        /// </summary>
        /// <param name="event">The optimization response event.</param>
        /// <param name="cancellationToken">The handler cancellation token.</param>
        /// <returns>A completed task after the pending request is completed.</returns>
        public Task Handle(PromptOptimizationResponseEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"收到 Prompt 优化响应: {@event.RequestId}, Score: {@event.Score}");
            
            _optimizationService.CompleteRequest(@event.RequestId, @event);
            
            return Task.CompletedTask;
        }
    }
}
