using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Senparc.Xncf.PromptRange.Application.EventHandlers
{
    public class PromptOptimizationRequestHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
    {
        private readonly PromptItemService _promptItemService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<PromptOptimizationRequestHandler> _logger;

        public PromptOptimizationRequestHandler(
            PromptItemService promptItemService,
            IEventBus eventBus,
            ILogger<PromptOptimizationRequestHandler> logger)
        {
            _promptItemService = promptItemService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"收到 Prompt 优化请求: {@event.RequestId}, Target: {@event.PromptCode}, Depth: {@event.Depth}, Chain: {@event.EventChain}");

            try
            {
                // 1. 获取原始 PromptItem
                // 这里假设 PromptItemService 有方法通过 Code 获取 Item，或者我们需要解析 Code (e.g. 2010.1.2.1-T1-A1)
                // 为简化，这里模拟生成一个新的 Prompt
                
                string newPromptContent = $"Optimized content based on: {@event.UserRequirement}. (Original Code: {@event.PromptCode})";
                
                // 2. 创建新的 PromptItem (这一步通常涉及 AI 生成，这里简化为直接创建)
                // 实际逻辑应该调用 AIKernel 生成优化后的 Prompt
                
                // 模拟生成的新 Code
                string newPromptCode = $"{@event.PromptCode}-Opt-{DateTime.Now.Ticks % 1000}";

                // 3. 发布响应事件（使用 PublishDerivedAsync 继承事件链）
                var responseEvent = new PromptOptimizationResponseEvent(
                    @event.RequestId,
                    newPromptCode,
                    newPromptContent,
                    new OptimizedParameters(
                        Temperature: @event.Context.CurrentTemperature,
                        TopP: @event.Context.CurrentTopP,
                        MaxTokens: @event.Context.CurrentMaxTokens,
                        FrequencyPenalty: @event.Context.CurrentFrequencyPenalty,
                        PresencePenalty: @event.Context.CurrentPresencePenalty
                    ),
                    0.85, // 模拟预测分数
                    "Optimization successful based on requirements.",
                    true,
                    ""
                );

                await _eventBus.PublishDerivedAsync(responseEvent, @event);
                _logger.LogInformation($"Prompt 优化完成，已发布响应: {newPromptCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prompt 优化失败");
                
                // 发布错误响应事件
                var errorResponse = new PromptOptimizationResponseEvent(
                    @event.RequestId,
                    null,
                    null,
                    null,
                    0,
                    ex.Message,
                    false,
                    ex.Message
                );
                
                await _eventBus.PublishDerivedAsync(errorResponse, @event);
            }
        }
    }
}
