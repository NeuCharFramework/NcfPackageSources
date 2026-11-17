/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptInitResponseHandler.cs
    File description: PromptInitResponseHandler related implementation
    
    
    Created by: Senparc - 20260306
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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

        public PromptInitResponseHandler(
            PromptOptimizationService optimizationService,
            ILogger<PromptInitResponseHandler> logger)
        {
            _optimizationService = optimizationService;
            _logger = logger;
        }

        public Task Handle(PromptInitResponseEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Prompt Init Response: {@event.PromptCode}, RequestId: {@event.RequestId}");
            _optimizationService.CompleteInitRequest(@event.RequestId, @event);
            return Task.CompletedTask;
        }
    }
}
