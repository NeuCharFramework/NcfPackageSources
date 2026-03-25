using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Senparc.Xncf.AgentsManager.Application.EventHandlers
{
    /// <summary>
    /// 监听 PromptOptimizationRequestEvent，创建 ChatTask 记录优化任务
    /// </summary>
    public class PromptOptimizationChatTaskHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
    {
        private readonly ChatGroupService _chatGroupService;
        private readonly ChatTaskService _chatTaskService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly ILogger<PromptOptimizationChatTaskHandler> _logger;

        public PromptOptimizationChatTaskHandler(
            ChatGroupService chatGroupService,
            ChatTaskService chatTaskService,
            AgentsTemplateService agentsTemplateService,
            ILogger<PromptOptimizationChatTaskHandler> logger)
        {
            _chatGroupService = chatGroupService;
            _chatTaskService = chatTaskService;
            _agentsTemplateService = agentsTemplateService;
            _logger = logger;
        }

        public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("========== PromptOptimizationChatTaskHandler 开始 ==========");
            _logger.LogInformation("  RequestId: {@event.RequestId}", @event.RequestId);

            try
            {
                // 1. 查找 PromptCatalyzer 的 ChatGroup
                _logger.LogInformation("【步骤1/3】查找 PromptCatalyzer ChatGroup...");
                var chatGroup = await _chatGroupService.GetObjectAsync(z => z.Name == "PromptCatalyzer-OptimizationGroup");
                
                if (chatGroup == null)
                {
                    _logger.LogWarning("  ⚠️  PromptCatalyzer ChatGroup 未找到，跳过 ChatTask 创建");
                    return;
                }
                
                _logger.LogInformation("  ✅ 找到 ChatGroup: {GroupId}, Name: {Name}", chatGroup.Id, chatGroup.Name);

                // 2. 获取 Agent 的 AIModelId（从 AgentTemplate 中查询）
                _logger.LogInformation("【步骤2/3】获取 Agent 信息...");
                var agent = await _agentsTemplateService.GetObjectAsync(z => z.Name == "PromptCatalyzer");
                
                if (agent == null)
                {
                    _logger.LogWarning("  ⚠️  PromptCatalyzer Agent 未找到，跳过 ChatTask 创建");
                    return;
                }
                
                // 从 Context 中提取 ModelId
                var aiModelId = @event.Context.ModelId;
                
                _logger.LogInformation("  ✅ Agent 信息：AgentId={AgentId}, AIModelId={ModelId}", agent.Id, aiModelId);

                // 3. 创建 ChatTask
                _logger.LogInformation("【步骤3/3】创建 ChatTask...");
                
                var chatTaskDto = new ChatTaskDto(
                    name: $"Prompt优化-{@event.PromptCode}",
                    chatGroupId: chatGroup.Id,
                    aiModelId: aiModelId,
                    status: ChatTask_Status.Chatting,
                    promptCommand: @event.UserRequirement,
                    description: $"优化 Prompt: {@event.PromptCode}",
                    isPersonality: false,
                    hookPlatform: HookPlatform.None,
                    hookPlatformParameter: null,
                    score: false,
                    startTime: DateTime.Now,
                    endTime: DateTime.Now,
                    resultComment: null
                );
                
                var chatTask = await _chatTaskService.CreateTask(chatTaskDto);
                
                _logger.LogInformation("  ✅ ChatTask 创建成功！TaskId: {TaskId}, Name: {Name}", 
                    chatTask.Id, chatTask.Name);
                
                // 注意：实际的 AI 优化逻辑由 PromptOptimizationRequestHandler 处理
                // 这里只负责记录任务到 ChatTask 表
                
                _logger.LogInformation("========== PromptOptimizationChatTaskHandler 完成 ==========");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ChatTask 创建失败");
                // 不抛出异常，避免影响主优化流程
            }
        }
    }
    
    /// <summary>
    /// 监听 PromptOptimizationResponseEvent，更新 ChatTask 状态
    /// </summary>
    public class PromptOptimizationTaskCompletionHandler : IIntegrationEventHandler<PromptOptimizationResponseEvent>
    {
        private readonly ChatTaskService _chatTaskService;
        private readonly ILogger<PromptOptimizationTaskCompletionHandler> _logger;

        public PromptOptimizationTaskCompletionHandler(
            ChatTaskService chatTaskService,
            ILogger<PromptOptimizationTaskCompletionHandler> logger)
        {
            _chatTaskService = chatTaskService;
            _logger = logger;
        }

        public async Task Handle(PromptOptimizationResponseEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("========== PromptOptimizationTaskCompletionHandler 开始 ==========");
            _logger.LogInformation("  RequestId: {@event.RequestId}, Success: {@event.Success}", @event.RequestId, @event.Success);

            try
            {
                // 根据 RequestId 查找对应的 ChatTask（通过 Name 模糊匹配）
                // 注意：这里的关联比较弱，因为 RequestId 没有直接存储在 ChatTask 中
                // 实际生产环境中可能需要更精确的关联机制
                
                var allTasks = await _chatTaskService.GetFullListAsync(
                    z => z.Status == ChatTask_Status.Chatting && z.Name.Contains("Prompt优化"),
                    z => z.AddTime,
                    Ncf.Core.Enums.OrderingType.Descending);
                
                if (allTasks != null && allTasks.Count > 0)
                {
                    // 找到最近的一个任务
                    var latestTask = allTasks[0];
                    
                    if (latestTask != null)
                    {
                        _logger.LogInformation("  找到对应的 ChatTask: {TaskId}, Name: {Name}", 
                            latestTask.Id, latestTask.Name);
                        
                        // 更新任务状态
                        var newStatus = @event.Success ? ChatTask_Status.Finished : ChatTask_Status.Cancelled;
                        await _chatTaskService.SetStatus(newStatus, latestTask);
                        
                        _logger.LogInformation("  ✅ ChatTask 状态已更新: {Status}", newStatus);
                    }
                }
                
                _logger.LogInformation("========== PromptOptimizationTaskCompletionHandler 完成 ==========");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 更新 ChatTask 状态失败");
                // 不抛出异常，避免影响主流程
            }
        }
    }
}
