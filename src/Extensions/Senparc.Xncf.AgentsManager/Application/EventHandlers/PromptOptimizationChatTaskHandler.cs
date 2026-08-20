/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptOptimizationChatTaskHandler.cs
    文件功能描述：PromptOptimizationChatTaskHandler 相关实现
    
    
    创建标识：Senparc - 20260325
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Senparc.Xncf.AgentsManager.Application.EventHandlers
{
    /// <summary>
    /// Prompt 优化唯一执行路径：运行 ChatGroup 直至 Agent 对话结束，再由 Plugin 写入结果并发布响应事件
    /// </summary>
    public class PromptOptimizationChatTaskHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
    {
        private readonly ChatGroupService _chatGroupService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly PromptOptimizationAgentBridge _bridge;
        private readonly PromptOptimizationKernelFallbackService _kernelFallback;
        private readonly IEventBus _eventBus;
        private readonly ILogger<PromptOptimizationChatTaskHandler> _logger;

        public PromptOptimizationChatTaskHandler(
            ChatGroupService chatGroupService,
            AgentsTemplateService agentsTemplateService,
            PromptOptimizationAgentBridge bridge,
            PromptOptimizationKernelFallbackService kernelFallback,
            IEventBus eventBus,
            ILogger<PromptOptimizationChatTaskHandler> logger)
        {
            _chatGroupService = chatGroupService;
            _agentsTemplateService = agentsTemplateService;
            _bridge = bridge;
            _kernelFallback = kernelFallback;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("========== PromptOptimizationChatTaskHandler（Agent 主路径）==========");
            _logger.LogInformation("  RequestId: {RequestId}", @event.RequestId);

            _bridge.BeginRequest(@event.RequestId);

            async Task PublishFailureAsync(string error)
            {
                _bridge.CleanupRequest(@event.RequestId);
                var err = new PromptOptimizationResponseEvent(
                    @event.RequestId,
                    null,
                    null,
                    null,
                    0,
                    error,
                    false,
                    error);
                await _eventBus.PublishDerivedAsync(err, @event);
            }

            try
            {
                var chatGroup = await _chatGroupService.GetObjectAsync(z => z.Name == "PromptCatalyzer-OptimizationGroup");
                if (chatGroup == null)
                {
                    await PublishFailureAsync("PromptCatalyzer ChatGroup 未找到，请先完成初始化。");
                    return;
                }

                var agent = await _agentsTemplateService.GetObjectAsync(z => z.Name == "PromptCatalyzer");
                if (agent == null)
                {
                    await PublishFailureAsync("PromptCatalyzer Agent 未找到，请先完成初始化。");
                    return;
                }

                var aiModelId = @event.Context.ModelId;
                var agentCommand = BuildAgentCommand(@event);

                _logger.LogInformation("  即将同步运行 ChatGroup 直至对话结束，ChatGroupId={GroupId}", chatGroup.Id);

                var runGroupRequest = new ChatGroup_RunGroupRequest
                {
                    ChatGroupId = chatGroup.Id,
                    AiModelId = aiModelId,
                    PromptCommand = agentCommand,
                    Name = $"Prompt优化-{@event.RequestId}",
                    Description = $"优化 Prompt: {@event.PromptCode}",
                    Personality = false,
                    HookPlatform = HookPlatform.None,
                    HookParameter = null,
                    CorrelationId = @event.RequestId
                };

                try
                {
                    await _chatGroupService.RunChatGroupAwaitAsync(runGroupRequest);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChatGroup 运行异常");
                    await PublishFailureAsync($"Agent 对话失败: {ex.Message}");
                    return;
                }

                if (_bridge.TryTakeResult(@event.RequestId, out var successResponse))
                {
                    await _eventBus.PublishDerivedAsync(successResponse, @event);
                    _logger.LogInformation("  ✅ 已发布优化成功响应，NewPromptCode={Code}", successResponse.NewPromptCode);
                }
                else
                {

                    _logger.LogWarning("  Agent 未通过工具创建版本，尝试 Kernel 回退…");
                    var fallbackResponse = await _kernelFallback.TryKernelFallbackAsync(@event, cancellationToken);
                    if (fallbackResponse.Success)
                    {
                        await _eventBus.PublishDerivedAsync(fallbackResponse, @event);
                        _logger.LogInformation("  ✅ Kernel 回退成功，NewPromptCode={Code}", fallbackResponse.NewPromptCode);
                    }
                    else
                    {
                        var detail = fallbackResponse.ErrorMessage ?? fallbackResponse.EvaluationReason;
                        await PublishFailureAsync(
                            string.IsNullOrWhiteSpace(detail)
                                ? "Agent 对话已结束，但未创建新版本；Kernel 回退也失败。请在 Agents 模块查看该 ChatTask 的对话与工具记录；服务器日志关键字：PromptOptimizationChatTaskHandler、CreateOptimizedPrompt、PromptOptimizationKernelFallbackService。"
                                : $"{detail}（若工具未调用，可对照 ChatTask 记录与上述日志关键字排查。）");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PromptOptimizationChatTaskHandler 未处理异常");
                await PublishFailureAsync(ex.Message);
            }
            finally
            {
                _bridge.CleanupRequest(@event.RequestId);
            }
        }

        private string BuildAgentCommand(PromptOptimizationRequestEvent @event)
        {
            var rangeName = @event.PromptCode?.Split('-')[0]?.Trim() ?? "";

            return $@"# Prompt 优化任务（仅通过工具完成，勿编造结果）

## 关联 REQUEST_ID（服务端已自动绑定；调用 CreateOptimizedPrompt 时第一个参数可留空）
{@event.RequestId}

## 当前基准 Prompt Code
{@event.PromptCode}

## 用户需求
{@event.UserRequirement}

## 基准 Prompt 全文（须在此基础上改进）
{@event.PromptContent}

## 参考参数（可调整）
- ModelId: {@event.Context.ModelId}
- Temperature: {@event.Context.CurrentTemperature}
- TopP: {@event.Context.CurrentTopP}
- MaxTokens: {@event.Context.CurrentMaxTokens}
- FrequencyPenalty: {@event.Context.CurrentFrequencyPenalty}
- PresencePenalty: {@event.Context.CurrentPresencePenalty}

## 执行步骤
1. 使用 GetPromptInfo 读取「{@event.PromptCode}」确认现状。
2. 使用 AnalyzeModelScores 分析 Range「{rangeName}」内模型历史评分，选择合适 ModelId（无历史时可沿用 {@event.Context.ModelId}）。
3. 根据用户需求改写 Prompt 正文与参数，准备最终 optimizedContent（多行文本须为真实换行，不要写字面量 \\n）。
4. **必须且仅能**调用 CreateOptimizedPrompt **一次**（服务端会拒绝同一次任务内的重复调用，避免生成多条版本）。先完成分析与改写，再一次性传入最终 optimizedContent 与参数。第一个参数 optimizationRequestId 可留空；务必传对 basePromptCode=""{@event.PromptCode}""。其余：optimizedContent、modelId、temperature、topP、maxTokens、frequencyPenalty、presencePenalty、improvementSummary。
5. **不要**调用 ExecuteShootTest / ExecuteAIGrade：打靶与 AI 评分将由 PromptRange 页面在用户可见流程中自动执行。
6. 一旦 CreateOptimizedPrompt 成功返回，**本任务即结束**，勿再次优化或再次调用该工具。

完成后用自然语言简要总结改动要点。";
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
            _logger.LogInformation("========== PromptOptimizationTaskCompletionHandler ==========");
            _logger.LogInformation("  RequestId: {RequestId}, Success: {Success}", @event.RequestId, @event.Success);

            try
            {
                var allTasks = await _chatTaskService.GetFullListAsync(
                    z => z.Status == ChatTask_Status.Chatting && z.Name != null && z.Name.Contains(@event.RequestId),
                    z => z.AddTime,
                    Ncf.Core.Enums.OrderingType.Descending);

                if (allTasks != null && allTasks.Count > 0)
                {
                    var latestTask = allTasks[0];
                    var newStatus = @event.Success ? ChatTask_Status.Finished : ChatTask_Status.Cancelled;
                    await _chatTaskService.SetStatus(newStatus, latestTask);
                    _logger.LogInformation("  ✅ ChatTask {TaskId} → {Status}", latestTask.Id, newStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 ChatTask 状态失败");
            }
        }
    }
}
