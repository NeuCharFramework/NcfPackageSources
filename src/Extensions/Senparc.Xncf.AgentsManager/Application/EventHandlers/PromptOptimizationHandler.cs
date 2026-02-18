using Senparc.Xncf.PromptRange.Abstractions.Events; // 引用 PromptRange 的事件
using System.Threading.Tasks;
using System.Threading;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.Application.EventHandlers
{
    // AgentsManager 现在知道 PromptRange 的存在，但只通过抽象层交互
    public class PromptOptimizationHandler : IIntegrationEventHandler<PromptTestFinishedEvent>
    {
        public async Task Handle(PromptTestFinishedEvent @event, CancellationToken cancellationToken)
        {
            // 处理逻辑...
        }
    }
}