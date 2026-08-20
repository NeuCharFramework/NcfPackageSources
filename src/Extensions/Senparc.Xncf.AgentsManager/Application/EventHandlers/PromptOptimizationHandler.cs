/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptOptimizationHandler.cs
    文件功能描述：PromptOptimizationHandler 相关实现
    
    
    创建标识：Senparc - 20260218
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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