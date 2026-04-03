using Senparc.Xncf.PromptRange.Abstractions.Events; // Events referencing PromptRange
using System.Threading.Tasks;
using System.Threading;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.Application.EventHandlers
{
    // AgentsManager is now aware of the existence of PromptRange but only interacts through the abstraction layer
    public class PromptOptimizationHandler : IIntegrationEventHandler<PromptTestFinishedEvent>
    {
        public async Task Handle(PromptTestFinishedEvent @event, CancellationToken cancellationToken)
        {
            // Processing logic...
        }
    }
}