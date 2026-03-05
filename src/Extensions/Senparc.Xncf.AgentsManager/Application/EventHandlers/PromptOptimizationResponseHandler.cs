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

        public PromptOptimizationResponseHandler(
            PromptOptimizationService optimizationService,
            ILogger<PromptOptimizationResponseHandler> logger)
        {
            _optimizationService = optimizationService;
            _logger= logger;
        }

        public Task Handle(PromptOptimizationResponseEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"收到 Prompt 优化响应: {@event.RequestId}, Score: {@event.Score}");
            
            _optimizationService.CompleteRequest(@event.RequestId, @event);
            
            return Task.CompletedTask;
        }
    }
}
