/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupService.cs
    文件功能描述：聊天组运行与多智能体协作编排服务
    
    
    创建标识：Senparc - 20240616
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 优化多智能体群聊任务管理与执行流程

    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Extensions;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.ACL;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

public class McpEndpoint
{
    public string url { get; set; }
}

public class ChatGroupService : ServiceBase<ChatGroup>
{
    private sealed class AgentRuntimeContext
    {
        public required AgentTemplate Template { get; init; }
        public required AgentTemplateDto TemplateDto { get; init; }
        public required ChatClientAgent Agent { get; init; }
        public required IWantToRun Runner { get; init; }
        public required ChatClientAgentOptions AgentOptions { get; init; }
        public SenparcAiSetting? Setting { get; init; }
    }

    private sealed class AgentRunResult
    {
        public bool HasOutput { get; init; }
        public string OutputText { get; init; } = string.Empty;
        public bool ShouldExit { get; init; }
    }

    private sealed class FallbackConversationTurn
    {
        public required string Speaker { get; init; }
        public required string Message { get; init; }
    }

    public static int ChatMaxRound = 20;
    public static List<Task> TaskList = new List<Task>();

    private readonly IBaseObjectCacheStrategy _cache;
    private readonly ChatTaskStreamHub _chatTaskStreamHub;

    public ChatGroupService(
        IRepositoryBase<ChatGroup> repo,
        IServiceProvider serviceProvider,
        IBaseObjectCacheStrategy cache,
        ChatTaskStreamHub chatTaskStreamHub)
        : base(repo, serviceProvider)
    {
        _cache = cache;
        _chatTaskStreamHub = chatTaskStreamHub;
    }

    /// <summary>
    /// 在独立进程中运行 ChatGroup（UI 界面中进行，不等待完成）
    /// </summary>
    public Task RunChatGroupInThread(ChatGroup_RunGroupRequest request)
    {
        var task = RunChatGroupExecutionCoreAsync(request);

        lock (TaskList)
        {
            TaskList.Add(task);
        }

        _ = task.ContinueWith(completedTask =>
        {
            lock (TaskList)
            {
                TaskList.Remove(completedTask);
            }

            if (completedTask.IsFaulted && completedTask.Exception != null)
            {
                SenparcTrace.BaseExceptionLog(completedTask.Exception.GetBaseException());
            }
        }, TaskScheduler.Default);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 运行 ChatGroup 直至本轮对话结束（用于 Prompt 优化等需同步等待的场景）
    /// </summary>
    public Task RunChatGroupAwaitAsync(ChatGroup_RunGroupRequest request)
    {
        return RunChatGroupExecutionCoreAsync(request);
    }

    private async Task RunChatGroupExecutionCoreAsync(ChatGroup_RunGroupRequest request)
    {
        IDisposable activeOptimizationScope = null;
        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            activeOptimizationScope = PromptOptimizationAgentBridge.BeginActiveRequestScope(request.CorrelationId);
            PromptOptimizationAgentBridge.SetFallbackCorrelationId(request.CorrelationId);
        }

        var scope = SenparcDI.GetServiceProvider(true).CreateScope();
        var services = scope.ServiceProvider;

        ChatTask chatTask = null;
        ChatTaskService chatTaskService = null;
        string runningKey = null;

        try
        {
            var groupId = request.ChatGroupId;
            var aiModelId = request.AiModelId;
            var personality = request.Personality;
            var userCommand = request.PromptCommand;
            var logger = new StringBuilder();

            var chatGroupMemberService = services.GetRequiredService<ChatGroupMemberService>();
            var agentTemplateService = services.GetRequiredService<AgentsTemplateService>();
            var promptItemService = services.GetRequiredService<PromptItemService>();
            chatTaskService = services.GetRequiredService<ChatTaskService>();
            var chatGroupHistoryService = services.GetRequiredService<ChatGroupHistoryService>();
            var chatGroupService = services.GetRequiredService<ChatGroupService>();

            var chatGroup = await chatGroupService.GetObjectAsync(x => x.Id == groupId);
            if (chatGroup == null)
            {
                throw new NcfExceptionBase($"聊天组不存在：{groupId}");
            }

            // 默认模型继承规则：未显式指定模型时，优先继承当前群主使用的 Prompt 模型。
            if (aiModelId <= 0)
            {
                var adminAgent = await agentTemplateService.GetObjectAsync(z => z.Id == chatGroup.AdminAgentTemplateId);
                if (adminAgent != null && !adminAgent.PromptCode.IsNullOrEmpty())
                {
                    try
                    {
                        var adminPrompt = await promptItemService.GetBestPromptAsync(adminAgent.PromptCode, true);
                        if (adminPrompt != null && adminPrompt.ModelId > 0)
                        {
                            aiModelId = adminPrompt.ModelId;
                        }
                    }
                    catch
                    {
                        // 保持 aiModelId=0，继续走系统默认模型。
                    }
                }
            }

            var chatGroupDto = chatGroupService.Mapping<ChatGroupDto>(chatGroup);
            var chatTaskDto = new ChatTaskDto(
                request.Name,
                groupId,
                aiModelId,
                ChatTask_Status.Waiting,
                userCommand,
                request.Description,
                personality,
                request.HookPlatform,
                request.HookParameter,
                false,
                DateTime.Now,
                DateTime.Now,
                null);

            chatTask = await chatTaskService.CreateTask(chatTaskDto);
            chatTaskDto = chatTaskService.Mapping<ChatTaskDto>(chatTask);
            await chatTaskService.SetStatus(ChatTask_Status.Chatting, chatTask);
            chatTaskDto = chatTaskService.Mapping<ChatTaskDto>(chatTask);

            runningKey = chatTaskService.GetChatTaskRunCacheKey(chatTask.Id);
            await _cache.SetAsync(runningKey, new RunningChatTaskDto { ChatTaskDto = chatTaskDto });
            PublishStatusEvent(chatTask.Id, ChatTask_Status.Chatting);

            logger.Append($"开始运行 {chatGroup.Name}");

            #region 确定 AiSetting

            var senparcAiSetting = Senparc.AI.Config.SenparcAiSetting;
            var aiModelService = services.GetRequiredService<AIModelService>();

            if (aiModelId != 0)
            {
                var aiModel = await aiModelService.GetObjectAsync(z => z.Id == aiModelId);
                if (aiModel == null)
                {
                    throw new NcfExceptionBase($"当前选择的 AI 模型不存在：{aiModelId}");
                }

                var aiModelDto = aiModelService.Mapper.Map<AIModelDto>(aiModel);
                senparcAiSetting = aiModelService.BuildSenparcAiSetting(aiModelDto);
            }
            else if (senparcAiSetting is SenparcAiSetting defaultSetting)
            {
                // 与 PromptRange 保持一致：将系统默认设置映射为 AIModel 再构建，统一 Endpoint/Deployment 归一化。
                var defaultModel = BuildModelDtoFromSetting(defaultSetting, "AgentsManager.RequestDefault");
                if (defaultModel != null)
                {
                    senparcAiSetting = aiModelService.BuildSenparcAiSetting(defaultModel);
                }
            }

            #endregion

            #region 收集成员（确保包含 Admin + Enter）

            var memberCollection = new List<(int AgentTemplateId, string Uid)>();
            var groupMembers = await chatGroupMemberService.GetFullListAsync(z => z.ChatGroupId == groupId, includes: "AgentTemplate");

            foreach (var member in groupMembers)
            {
                if (member.AgentTemplate.Enable is false)
                {
                    logger.AppendLine($"智能体【{member.AgentTemplate.Name}】目前为关闭状态，跳过对话");
                    continue;
                }

                memberCollection.Add((member.AgentTemplateId, member.UID));
            }

            if (groupMembers.All(z => z.AgentTemplateId != chatGroup.AdminAgentTemplateId))
            {
                memberCollection.Add((chatGroup.AdminAgentTemplateId, "Admin"));
            }

            if (groupMembers.All(z => z.AgentTemplateId != chatGroup.EnterAgentTemplateId))
            {
                memberCollection.Add((chatGroup.EnterAgentTemplateId, "Enter"));
            }

            memberCollection = memberCollection
                .GroupBy(m => m.AgentTemplateId)
                .Select(g => g.First())
                .ToList();

            #endregion

            var aiPlugins = AIPluginHub.Instance;
            var runtimeContexts = new List<AgentRuntimeContext>();

            foreach (var member in memberCollection)
            {
                var template = await agentTemplateService.GetObjectAsync(x => x.Id == member.AgentTemplateId);
                if (template == null || !template.Enable)
                {
                    continue;
                }

                var templateDto = agentTemplateService.Mapper.Map<AgentTemplateDto>(template);
                var (agentPrompt, currentAgentSetting) = await ResolvePromptAndSettingAsync(
                    promptItemService,
                    templateDto,
                    template,
                    aiModelService,
                    senparcAiSetting);

                var effectiveSetting = personality ? currentAgentSetting : senparcAiSetting;
                var agentHandler = new AgentAiHandler(effectiveSetting);

                var tools = await BuildAgentToolsAsync(services, aiPlugins, agentHandler, templateDto, template.Id);

                var chatOptions = new ChatOptions
                {
                    Instructions = agentPrompt,
                    MaxOutputTokens = 2000,
                    Temperature = 0.3f,
                    TopP = 0.3f,
                    AllowMultipleToolCalls = tools.Count > 0
                };

                if (tools.Count > 0)
                {
                    chatOptions.Tools = tools.Cast<AITool>().ToList();
                }

                var agentOptions = new ChatClientAgentOptions
                {
                    Name = template.Name,
                    Description = string.IsNullOrWhiteSpace(template.Description) ? template.SystemMessage : template.Description,
                    ChatOptions = chatOptions
                };

                var runner = await agentHandler
                    .IWantTo(effectiveSetting)
                    .ConfigChatModel($"{template.Name}-{member.Uid}", agentOptions)
                    .BuildKernelWithAgentSessionAsync();

                runtimeContexts.Add(new AgentRuntimeContext
                {
                    Template = template,
                    TemplateDto = templateDto,
                    Agent = runner.Kernel.ChatClientAgent,
                    Runner = runner,
                    AgentOptions = CloneAgentOptions(agentOptions),
                    Setting = effectiveSetting as SenparcAiSetting
                });
            }

            if (runtimeContexts.Count == 0)
            {
                throw new NcfExceptionBase($"聊天组【{chatGroup.Name}】没有可用的启用智能体。");
            }

            var adminContext = runtimeContexts.FirstOrDefault(z => z.Template.Id == chatGroup.AdminAgentTemplateId)
                ?? throw new NcfExceptionBase($"聊天组【{chatGroup.Name}】未找到有效群主智能体（ID：{chatGroup.AdminAgentTemplateId}）。");

            var enterContext = runtimeContexts.FirstOrDefault(z => z.Template.Id == chatGroup.EnterAgentTemplateId)
                ?? adminContext;

            if (runtimeContexts.Count == 1)
            {
                var singleAgentResult = await RunSingleAgentAsync(
                    enterContext,
                    userCommand,
                    aiModelService,
                    logger,
                    chatGroup,
                    chatGroupDto,
                    chatTaskDto,
                    chatGroupHistoryService,
                    runningKey,
                    chatTask,
                    chatTaskService,
                    1);

                if (!singleAgentResult.HasOutput)
                {
                    logger.AppendLine($"[{chatGroup.Name}] 单智能体执行未返回可显示文本。");
                }

                if (chatTask.Status == ChatTask_Status.Cancelled)
                {
                    await _cache.RemoveFromCacheAsync(runningKey);
                    return;
                }

                await chatTaskService.SetStatus(ChatTask_Status.Finished, chatTask);
                await _cache.RemoveFromCacheAsync(runningKey);
                PublishStatusEvent(chatTask.Id, ChatTask_Status.Finished);
                SenparcTrace.SendCustomLog($"Agents 运行结果（组：{chatGroup.Name}）", logger.ToString());
                return;
            }

            var maxWorkflowTurns = request.ChatMaxRound > 0 ? request.ChatMaxRound : ChatMaxRound;
            var effectiveWorkflowTurns = Math.Max(2, maxWorkflowTurns);
            var multiAgentContexts = runtimeContexts
                .OrderByDescending(z => z.Template.Id == adminContext.Template.Id)
                .ThenByDescending(z => z.Template.Id == enterContext.Template.Id)
                .ToList();

            if (multiAgentContexts.Count < 2)
            {
                throw new NcfExceptionBase($"聊天组【{chatGroup.Name}】多智能体协作至少需要两个启用智能体。");
            }

            #region Microsoft Agent Framework 多智能体工作流

#pragma warning disable MAAIW001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            Workflow workflow = AgentWorkflowBuilder
                .CreateGroupChatBuilderWith(agents =>
                {
                    var manager = new RoundRobinGroupChatManager(
                        agents,
                        async (_, history, _) =>
                        {
                            var latestText = history?
                                .Reverse()
                                .Select(ExtractChatMessageText)
                                .FirstOrDefault(z => !string.IsNullOrWhiteSpace(z));

                            return IsExitSignal(latestText);
                        });

                    manager.MaximumIterationCount = effectiveWorkflowTurns;
                    return manager;
                })
                .AddParticipants(multiAgentContexts.Select(z => z.Agent).ToList())
                .WithName(chatGroup.Name)
                .WithDescription(string.IsNullOrWhiteSpace(chatGroup.Description) ? "AgentsManager ChatGroup Workflow" : chatGroup.Description)
                .Build();

            var taskPrompt = BuildWorkflowPrompt(adminContext.Agent.Name, multiAgentContexts.Select(z => z.Agent.Name), userCommand);

            var contextByExecutorId = BuildRuntimeContextIndex(runtimeContexts);

            string activeResponseKey = null;
            string activeExecutorId = null;
            var activeResponseText = new StringBuilder();
            UsageDetails activeUsageDetails = null;
            DateTime? activeResponseStartedAt = null;
            var roundIndex = 0;
            var shouldExit = false;
            var finalizedResponseKeys = new HashSet<string>(StringComparer.Ordinal);
            var streamedResponseKeys = new HashSet<string>(StringComparer.Ordinal);
            var observedWorkflowEventTypes = new HashSet<string>(StringComparer.Ordinal);
            var workflowFailed = false;
            var workflowFailureReason = string.Empty;

            async Task PersistAgentResponseAsync(
                string responseKey,
                string executorId,
                string messageText,
                UsageDetails usageDetails,
                DateTime? responseStartedAt)
            {
                var normalizedText = (messageText ?? string.Empty).Trim();
                if (normalizedText.Length == 0 || string.IsNullOrWhiteSpace(executorId))
                {
                    return;
                }

                responseKey ??= Guid.NewGuid().ToString("n");

                if (!string.IsNullOrWhiteSpace(responseKey)
                    && finalizedResponseKeys.Contains(responseKey))
                {
                    return;
                }

                roundIndex += 1;
                var responseMilliseconds = responseStartedAt.HasValue
                    ? Math.Max(0, (int)Math.Round((DateTime.Now - responseStartedAt.Value).TotalMilliseconds))
                    : 0;

                var usageSnapshot = BuildUsageSnapshot(
                    usageDetails,
                    responseMilliseconds,
                    roundIndex,
                    responseKey);

                if (TryResolveRuntimeContext(contextByExecutorId, executorId, out var speakerContext))
                {
                    if (!string.IsNullOrWhiteSpace(responseKey)
                        && !streamedResponseKeys.Contains(responseKey))
                    {
                        await PublishSyntheticChunkEventsAsync(
                            chatTask.Id,
                            speakerContext.TemplateDto.Id,
                            speakerContext.Agent.Name,
                            responseKey,
                            normalizedText,
                            roundIndex);
                        streamedResponseKeys.Add(responseKey);
                    }

                    var history = await SaveAgentMessageAsync(
                        speakerContext,
                        normalizedText,
                        chatGroup,
                        chatGroupDto,
                        chatTaskDto,
                        chatGroupHistoryService,
                        chatTask,
                        chatTaskService,
                        logger,
                        usageSnapshot);

                    PublishMessageEvent(
                        chatTask.Id,
                        history?.Id,
                        speakerContext.TemplateDto.Id,
                        speakerContext.Agent.Name,
                        responseKey,
                        normalizedText,
                        usageSnapshot);
                }
                else
                {
                    logger.AppendLine($"[{chatGroup.Name}]组 {executorId} 发送消息：{normalizedText}");
                }

                if (!string.IsNullOrWhiteSpace(responseKey))
                {
                    finalizedResponseKeys.Add(responseKey);
                }

                if (IsExitSignal(normalizedText))
                {
                    shouldExit = true;
                }
            }

            async Task FlushActiveResponseAsync()
            {
                if (activeResponseText.Length == 0 || string.IsNullOrWhiteSpace(activeExecutorId))
                {
                    activeResponseText.Clear();
                    activeResponseKey = null;
                    activeExecutorId = null;
                    activeUsageDetails = null;
                    activeResponseStartedAt = null;
                    return;
                }

                await PersistAgentResponseAsync(
                    activeResponseKey,
                    activeExecutorId,
                    activeResponseText.ToString(),
                    activeUsageDetails,
                    activeResponseStartedAt);

                activeResponseText.Clear();
                activeResponseKey = null;
                activeExecutorId = null;
                activeUsageDetails = null;
                activeResponseStartedAt = null;
            }

            StreamingRun run = null;
            try
            {
                run = await InProcessExecution.RunStreamingAsync(
                    workflow,
                    new List<ChatMessage> { new(ChatRole.User, taskPrompt) });

                var emittedTurnTokens = 0;
                var idleSuperStepCount = 0;

                while (!shouldExit)
                {
                    var keepRunning = await _cache.GetAsync<RunningChatTaskDto>(runningKey);
                    if (keepRunning == null)
                    {
                        logger.Append($"任务已被强制停止：{chatTask.Name}");
                        await FlushActiveResponseAsync();
                        await chatTaskService.SetStatus(ChatTask_Status.Cancelled, chatTask);
                        PublishStatusEvent(chatTask.Id, ChatTask_Status.Cancelled);
                        return;
                    }

                    var turnTokenAccepted = await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
                    if (turnTokenAccepted)
                    {
                        emittedTurnTokens += 1;
                    }
                    else
                    {
                        var rejectedStatus = await run.GetStatusAsync();
                        if (rejectedStatus == RunStatus.Ended)
                        {
                            break;
                        }

                        logger.AppendLine($"[{chatGroup.Name}] TurnToken 未被接收，当前状态：{rejectedStatus}");
                    }

                    var hadEventsInCurrentSuperStep = false;

                    await foreach (var workflowEvent in run.WatchStreamAsync())
                    {
                        hadEventsInCurrentSuperStep = true;

                        var loopKeepRunning = await _cache.GetAsync<RunningChatTaskDto>(runningKey);
                        if (loopKeepRunning == null)
                        {
                            logger.Append($"任务已被强制停止：{chatTask.Name}");
                            await FlushActiveResponseAsync();
                            await chatTaskService.SetStatus(ChatTask_Status.Cancelled, chatTask);
                            PublishStatusEvent(chatTask.Id, ChatTask_Status.Cancelled);
                            return;
                        }

                        switch (workflowEvent)
                        {
                            case AgentResponseUpdateEvent updateEvent:
                                {
                                    var responseId = updateEvent.Update.ResponseId
                                                     ?? updateEvent.Update.MessageId;

                                    if (string.IsNullOrWhiteSpace(responseId))
                                    {
                                        responseId = string.Equals(activeExecutorId, updateEvent.ExecutorId, StringComparison.OrdinalIgnoreCase)
                                            && !string.IsNullOrWhiteSpace(activeResponseKey)
                                            ? activeResponseKey
                                            : Guid.NewGuid().ToString("n");
                                    }

                                    if (!string.Equals(activeResponseKey, responseId, StringComparison.Ordinal))
                                    {
                                        await FlushActiveResponseAsync();
                                        activeResponseKey = responseId;
                                        activeExecutorId = updateEvent.ExecutorId;
                                        activeUsageDetails = null;
                                        activeResponseStartedAt = DateTime.Now;
                                    }

                                    var updateText = ExtractAgentResponseUpdateText(updateEvent.Update);
                                    if (!string.IsNullOrEmpty(updateText))
                                    {
                                        activeResponseText.Append(updateText);

                                        if (TryResolveRuntimeContext(contextByExecutorId, updateEvent.ExecutorId, out var streamContext))
                                        {
                                            streamedResponseKeys.Add(responseId);
                                            PublishChunkEvent(
                                                chatTask.Id,
                                                streamContext.TemplateDto.Id,
                                                streamContext.Agent.Name,
                                                responseId,
                                                updateText,
                                                roundIndex + 1);
                                        }
                                    }

                                    var usageContent = updateEvent.Update.Contents?
                                        .FirstOrDefault(z => z is UsageContent) as UsageContent;
                                    if (usageContent?.Details != null)
                                    {
                                        activeUsageDetails = usageContent.Details;
                                    }

                                    break;
                                }
                            case AgentResponseEvent responseEvent:
                                {
                                    var executorId = responseEvent.ExecutorId;
                                    var response = responseEvent.Response;
                                    if (response == null || string.IsNullOrWhiteSpace(executorId))
                                    {
                                        break;
                                    }

                                    var responseId = response.ResponseId;
                                    if (string.IsNullOrWhiteSpace(responseId))
                                    {
                                        responseId = string.Equals(activeExecutorId, executorId, StringComparison.OrdinalIgnoreCase)
                                            && !string.IsNullOrWhiteSpace(activeResponseKey)
                                            ? activeResponseKey
                                            : Guid.NewGuid().ToString("n");
                                    }

                                    var responseText = ExtractAgentResponseText(response);

                                    if (string.Equals(activeResponseKey, responseId, StringComparison.Ordinal))
                                    {
                                        if (activeUsageDetails == null && response.Usage != null)
                                        {
                                            activeUsageDetails = response.Usage;
                                        }

                                        if (activeResponseText.Length == 0 && !string.IsNullOrWhiteSpace(responseText))
                                        {
                                            activeResponseText.Append(responseText);
                                        }

                                        await FlushActiveResponseAsync();
                                        break;
                                    }

                                    await FlushActiveResponseAsync();
                                    await PersistAgentResponseAsync(
                                        responseId,
                                        executorId,
                                        responseText,
                                        response.Usage,
                                        DateTime.Now);

                                    break;
                                }
                            case WorkflowOutputEvent outputEvent:
                                {
                                    var executorId = outputEvent.ExecutorId;
                                    if (string.IsNullOrWhiteSpace(executorId))
                                    {
                                        break;
                                    }

                                    switch (outputEvent.Data)
                                    {
                                        case AgentResponse outputResponse:
                                            {
                                                var outputResponseId = outputResponse.ResponseId;
                                                if (string.IsNullOrWhiteSpace(outputResponseId))
                                                {
                                                    outputResponseId = Guid.NewGuid().ToString("n");
                                                }

                                                await FlushActiveResponseAsync();
                                                await PersistAgentResponseAsync(
                                                    outputResponseId,
                                                    executorId,
                                                    ExtractAgentResponseText(outputResponse),
                                                    outputResponse.Usage,
                                                    DateTime.Now);
                                                break;
                                            }
                                        case AgentResponseUpdate outputUpdate:
                                            {
                                                var responseId = outputUpdate.ResponseId
                                                                 ?? outputUpdate.MessageId;

                                                if (string.IsNullOrWhiteSpace(responseId))
                                                {
                                                    responseId = string.Equals(activeExecutorId, executorId, StringComparison.OrdinalIgnoreCase)
                                                        && !string.IsNullOrWhiteSpace(activeResponseKey)
                                                        ? activeResponseKey
                                                        : Guid.NewGuid().ToString("n");
                                                }

                                                if (!string.Equals(activeResponseKey, responseId, StringComparison.Ordinal))
                                                {
                                                    await FlushActiveResponseAsync();
                                                    activeResponseKey = responseId;
                                                    activeExecutorId = executorId;
                                                    activeUsageDetails = null;
                                                    activeResponseStartedAt = DateTime.Now;
                                                }

                                                var updateText = ExtractAgentResponseUpdateText(outputUpdate);
                                                if (!string.IsNullOrEmpty(updateText))
                                                {
                                                    activeResponseText.Append(updateText);
                                                    if (TryResolveRuntimeContext(contextByExecutorId, executorId, out var streamContext))
                                                    {
                                                        streamedResponseKeys.Add(responseId);
                                                        PublishChunkEvent(
                                                            chatTask.Id,
                                                            streamContext.TemplateDto.Id,
                                                            streamContext.Agent.Name,
                                                            responseId,
                                                            updateText,
                                                            roundIndex + 1);
                                                    }
                                                }

                                                if (outputUpdate.Contents?.FirstOrDefault(z => z is UsageContent) is UsageContent outputUsage
                                                    && outputUsage.Details != null)
                                                {
                                                    activeUsageDetails = outputUsage.Details;
                                                }
                                            }
                                            break;
                                        case ChatMessage outputMessage:
                                            await FlushActiveResponseAsync();
                                            await PersistAgentResponseAsync(
                                                Guid.NewGuid().ToString("n"),
                                                executorId,
                                                ExtractChatMessageText(outputMessage),
                                                null,
                                                DateTime.Now);
                                            break;
                                        case string outputText:
                                            await FlushActiveResponseAsync();
                                            await PersistAgentResponseAsync(
                                                Guid.NewGuid().ToString("n"),
                                                executorId,
                                                outputText,
                                                null,
                                                DateTime.Now);
                                            break;
                                    }

                                    break;
                                }
                            case WorkflowErrorEvent workflowError:
                                workflowFailed = true;
                                workflowFailureReason = workflowError.Exception?.Message
                                    ?? "Workflow execution failed.";
                                logger.AppendLine($"[{chatGroup.Name}] 工作流错误：{workflowFailureReason}");
                                shouldExit = true;
                                break;
                            case ExecutorFailedEvent executorFailed:
                                workflowFailed = true;
                                workflowFailureReason = $"Executor '{executorFailed.ExecutorId}' failed: {executorFailed.Data}";
                                logger.AppendLine($"[{chatGroup.Name}] 执行器错误：{workflowFailureReason}");
                                shouldExit = true;
                                break;
                            default:
                                {
                                    var eventTypeName = workflowEvent.GetType().Name;
                                    if (observedWorkflowEventTypes.Add(eventTypeName))
                                    {
                                        logger.AppendLine($"[{chatGroup.Name}] 收到事件：{eventTypeName}");
                                    }
                                }
                                break;
                        }

                        if (shouldExit)
                        {
                            break;
                        }
                    }

                    if (shouldExit)
                    {
                        break;
                    }

                    var runStatus = await run.GetStatusAsync();
                    if (runStatus == RunStatus.Ended)
                    {
                        break;
                    }

                    if (runStatus == RunStatus.PendingRequests)
                    {
                        workflowFailed = true;
                        workflowFailureReason = "多智能体工作流等待外部响应，当前执行路径无法自动继续。";
                        logger.AppendLine($"[{chatGroup.Name}] {workflowFailureReason}");
                        shouldExit = true;
                        break;
                    }

                    if (hadEventsInCurrentSuperStep)
                    {
                        idleSuperStepCount = 0;
                    }
                    else
                    {
                        idleSuperStepCount += 1;
                    }

                    if (idleSuperStepCount >= 2)
                    {
                        logger.AppendLine($"[{chatGroup.Name}] 连续两轮未收到新事件，结束本次工作流驱动。");
                        break;
                    }

                    if (emittedTurnTokens >= effectiveWorkflowTurns)
                    {
                        logger.AppendLine($"[{chatGroup.Name}] 已达到最大轮次限制（{effectiveWorkflowTurns}），结束本次协作。");
                        break;
                    }
                }
            }
            catch (Exception workflowEx)
            {
                workflowFailed = true;
                workflowFailureReason = workflowEx.Message;
                logger.AppendLine($"[{chatGroup.Name}] 工作流执行异常：{workflowFailureReason}");
            }
            finally
            {
                if (run != null)
                {
                    await run.DisposeAsync();
                }

                await FlushActiveResponseAsync();
            }

            if (roundIndex == 0 || workflowFailed)
            {
                if (workflowFailed)
                {
                    logger.AppendLine($"[{chatGroup.Name}] 多智能体工作流中断，自动降级为顺序轮转回退执行。");
                }
                else
                {
                    logger.AppendLine($"[{chatGroup.Name}] 多智能体工作流未返回可展示内容，自动降级为顺序轮转回退执行。");
                }

                var hasFallbackOutput = await RunMultiAgentFallbackAsync(
                    multiAgentContexts,
                    enterContext,
                    userCommand,
                    aiModelService,
                    logger,
                    chatGroup,
                    chatGroupDto,
                    chatTaskDto,
                    chatGroupHistoryService,
                    runningKey,
                    chatTask,
                    chatTaskService,
                    Math.Max(1, roundIndex + 1),
                    effectiveWorkflowTurns);

                if (!hasFallbackOutput && workflowFailed)
                {
                    var fallbackNotice = $"系统提示：多智能体编排失败（{workflowFailureReason}），且降级执行未返回可显示文本。";
                    await PersistAgentResponseAsync(
                        Guid.NewGuid().ToString("n"),
                        enterContext.Agent.Name,
                        fallbackNotice,
                        null,
                        DateTime.Now);
                }
            }
#pragma warning restore MAAIW001

            #endregion

            logger.Append("已完成运行：" + chatGroup.Name);
            await chatTaskService.SetStatus(ChatTask_Status.Finished, chatTask);
            await _cache.RemoveFromCacheAsync(runningKey);
            PublishStatusEvent(chatTask.Id, ChatTask_Status.Finished);

            SenparcTrace.SendCustomLog($"Agents 运行结果（组：{chatGroup.Name}）", logger.ToString());
        }
        catch (Exception ex)
        {
            SenparcTrace.BaseExceptionLog(ex);
            SenparcTrace.SendCustomLog("异常详情", ex.StackTrace);

            if (chatTask != null && chatTaskService != null)
            {
                try
                {
                    await chatTaskService.SetStatus(ChatTask_Status.Cancelled, chatTask);
                    PublishStatusEvent(chatTask.Id, ChatTask_Status.Cancelled);
                }
                catch
                {
                    // 忽略收尾异常，保留原始异常抛出
                }
            }

            if (!string.IsNullOrWhiteSpace(runningKey))
            {
                try
                {
                    await _cache.RemoveFromCacheAsync(runningKey);
                }
                catch
                {
                    // 忽略收尾异常，保留原始异常抛出
                }
            }

            throw;
        }
        finally
        {
            scope.Dispose();
            activeOptimizationScope?.Dispose();

            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                PromptOptimizationAgentBridge.ClearFallbackCorrelationId();
            }
        }
    }

    private static string BuildWorkflowPrompt(string enterAgentName, IEnumerable<string> participantNames, string userCommand)
    {
        var members = string.Join("、", participantNames.Where(z => !string.IsNullOrWhiteSpace(z)));
        return $@"请组织一个多智能体协作会话，并完成用户需求。

入口智能体：{enterAgentName}
可参与智能体：{members}

流程要求：
1. 首轮由入口智能体先回复“已就位”，并简短复述用户需求。
2. 后续由最合适的智能体继续协作。
3. 结束前给出明确结论或可执行方案。

用户需求：
{userCommand}";
    }

    private static Dictionary<string, AgentRuntimeContext> BuildRuntimeContextIndex(
        IEnumerable<AgentRuntimeContext> runtimeContexts)
    {
        var index = new Dictionary<string, AgentRuntimeContext>(StringComparer.OrdinalIgnoreCase);

        static void TryAdd(
            IDictionary<string, AgentRuntimeContext> target,
            string key,
            AgentRuntimeContext context)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!target.ContainsKey(key))
            {
                target[key] = context;
            }
        }

        foreach (var context in runtimeContexts)
        {
            TryAdd(index, context.Agent.Id, context);
            TryAdd(index, context.Agent.Name, context);
            TryAdd(index, context.Template.Name, context);
            TryAdd(index, context.TemplateDto.Name, context);
        }

        return index;
    }

    private static bool TryResolveRuntimeContext(
        IReadOnlyDictionary<string, AgentRuntimeContext> contextIndex,
        string executorId,
        out AgentRuntimeContext context)
    {
        context = default!;
        if (string.IsNullOrWhiteSpace(executorId))
        {
            return false;
        }

        if (contextIndex.TryGetValue(executorId, out context))
        {
            return true;
        }

        foreach (var item in contextIndex)
        {
            var key = item.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (executorId.EndsWith(key, StringComparison.OrdinalIgnoreCase)
                || key.EndsWith(executorId, StringComparison.OrdinalIgnoreCase))
            {
                context = item.Value;
                return true;
            }
        }

        return false;
    }

    private static bool IsExitSignal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        normalized = normalized.TrimEnd('.', '!', '?', ';', ':', '。', '！', '？', '；', '：', ']', '>');
        normalized = normalized.TrimStart('[', '<');

        return normalized.Equals("exit", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("结束", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("退出", StringComparison.OrdinalIgnoreCase);
    }

    private static List<AgentRuntimeContext> BuildFallbackRoundRobinSequence(
        IReadOnlyCollection<AgentRuntimeContext> participantContexts,
        AgentRuntimeContext enterContext)
    {
        var sequence = new List<AgentRuntimeContext>();

        if (enterContext != null)
        {
            sequence.Add(enterContext);
        }

        foreach (var context in participantContexts)
        {
            if (sequence.Any(z => z.Template.Id == context.Template.Id))
            {
                continue;
            }

            sequence.Add(context);
        }

        return sequence;
    }

    private static string BuildRoundRobinFallbackPrompt(
        string userCommand,
        IReadOnlyCollection<FallbackConversationTurn> history,
        string currentSpeaker)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你正在继续一个多智能体协作会话。");
        sb.AppendLine($"当前轮到你（{currentSpeaker}）发言。");
        sb.AppendLine("请基于已有对话继续推进，不要重复前文。若任务已完成，请只输出“exit”。");
        sb.AppendLine();
        sb.AppendLine("用户需求：");
        sb.AppendLine(userCommand);

        if (history.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("已有对话摘录：");
            foreach (var turn in history.TakeLast(6))
            {
                var message = turn.Message?.Trim() ?? string.Empty;
                if (message.Length > 220)
                {
                    message = message[..220] + "...";
                }

                sb.AppendLine($"{turn.Speaker}: {message}");
            }
        }

        return sb.ToString();
    }

    private async Task<bool> RunMultiAgentFallbackAsync(
        IReadOnlyCollection<AgentRuntimeContext> participantContexts,
        AgentRuntimeContext enterContext,
        string userCommand,
        AIModelService aiModelService,
        StringBuilder logger,
        ChatGroup chatGroup,
        ChatGroupDto chatGroupDto,
        ChatTaskDto chatTaskDto,
        ChatGroupHistoryService chatGroupHistoryService,
        string runningKey,
        ChatTask chatTask,
        ChatTaskService chatTaskService,
        int startRoundIndex,
        int maxRoundCount)
    {
        var sequence = BuildFallbackRoundRobinSequence(participantContexts, enterContext);
        if (sequence.Count == 0)
        {
            return false;
        }

        var totalRounds = Math.Max(2, maxRoundCount);
        var hasOutput = false;
        var conversation = new List<FallbackConversationTurn>();

        for (var i = 0; i < totalRounds; i++)
        {
            var context = sequence[i % sequence.Count];
            var prompt = i == 0
                ? userCommand
                : BuildRoundRobinFallbackPrompt(userCommand, conversation, context.Agent.Name);

            var roundIndex = startRoundIndex + i;
            var result = await RunSingleAgentAsync(
                context,
                prompt,
                aiModelService,
                logger,
                chatGroup,
                chatGroupDto,
                chatTaskDto,
                chatGroupHistoryService,
                runningKey,
                chatTask,
                chatTaskService,
                roundIndex);

            if (result.HasOutput)
            {
                hasOutput = true;
                conversation.Add(new FallbackConversationTurn
                {
                    Speaker = context.Agent.Name,
                    Message = result.OutputText
                });
            }

            if (result.ShouldExit)
            {
                logger.AppendLine($"[{chatGroup.Name}] 回退轮转检测到退出信号，提前结束。");
                break;
            }
        }

        return hasOutput;
    }

    private async Task<AgentRunResult> RunSingleAgentAsync(
        AgentRuntimeContext agentContext,
        string userCommand,
        AIModelService aiModelService,
        StringBuilder logger,
        ChatGroup chatGroup,
        ChatGroupDto chatGroupDto,
        ChatTaskDto chatTaskDto,
        ChatGroupHistoryService chatGroupHistoryService,
        string runningKey,
        ChatTask chatTask,
        ChatTaskService chatTaskService,
        int roundIndex)
    {
        var keepRunning = await _cache.GetAsync<RunningChatTaskDto>(runningKey);
        if (keepRunning == null)
        {
            logger.Append($"任务已被强制停止：{chatTask.Name}");
            await chatTaskService.SetStatus(ChatTask_Status.Cancelled, chatTask);
            PublishStatusEvent(chatTask.Id, ChatTask_Status.Cancelled);
            return new AgentRunResult
            {
                HasOutput = false,
                OutputText = string.Empty,
                ShouldExit = false
            };
        }

        UsageDetails streamUsageDetails = null;
        var responseId = Guid.NewGuid().ToString("n");
        var responseStartedAt = DateTime.Now;
        var streamedOutput = new StringBuilder();
        var hasStreamedChunk = false;

        try
        {
            var result = await ExecuteRunnerWithSessionRetryAsync(
                agentContext.Runner,
                userCommand,
                update =>
                {
                    var updateText = ExtractAgentResponseUpdateText(update);
                    if (!string.IsNullOrEmpty(updateText))
                    {
                        streamedOutput.Append(updateText);
                        hasStreamedChunk = true;
                        PublishChunkEvent(
                            chatTask.Id,
                            agentContext.TemplateDto.Id,
                            agentContext.Agent.Name,
                            responseId,
                            updateText,
                            roundIndex);
                    }

                    if (update?.Contents?.FirstOrDefault(z => z is UsageContent) is UsageContent usageContent
                        && usageContent.Details != null)
                    {
                        streamUsageDetails = usageContent.Details;
                    }
                });
            var output = string.IsNullOrWhiteSpace(result?.OutputString)
                ? streamedOutput.ToString()
                : result.OutputString;

            if (ContainsServiceFailureSignature(output))
            {
                throw new NcfExceptionBase(output.Trim());
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                var usageSnapshot = BuildUsageSnapshot(
                    streamUsageDetails ?? result?.Result?.Usage,
                    Math.Max(0, (int)Math.Round((DateTime.Now - responseStartedAt).TotalMilliseconds)),
                    roundIndex,
                    responseId);

                if (!hasStreamedChunk)
                {
                    await PublishSyntheticChunkEventsAsync(
                        chatTask.Id,
                        agentContext.TemplateDto.Id,
                        agentContext.Agent.Name,
                        responseId,
                        output,
                        roundIndex);
                    hasStreamedChunk = true;
                }

                var history = await SaveAgentMessageAsync(
                    agentContext,
                    output,
                    chatGroup,
                    chatGroupDto,
                    chatTaskDto,
                    chatGroupHistoryService,
                    chatTask,
                    chatTaskService,
                    logger,
                    usageSnapshot);

                PublishMessageEvent(
                    chatTask.Id,
                    history?.Id,
                    agentContext.TemplateDto.Id,
                    agentContext.Agent.Name,
                    responseId,
                    output,
                    usageSnapshot);

                return new AgentRunResult
                {
                    HasOutput = true,
                    OutputText = output,
                    ShouldExit = IsExitSignal(output)
                };
            }

            return new AgentRunResult
            {
                HasOutput = false,
                OutputText = string.Empty,
                ShouldExit = false
            };
        }
        catch (Exception ex)
        {
            if (ContainsForbiddenStatus(ex))
            {
                var fallbackResult = await TryRunSingleAgentForbiddenFallbackAsync(
                    agentContext,
                    userCommand,
                    aiModelService,
                    chatGroup.Name);

                if (fallbackResult.Success && !string.IsNullOrWhiteSpace(fallbackResult.Output))
                {
                    logger.AppendLine($"[{chatGroup.Name}] 单智能体 403 回退成功：{fallbackResult.FallbackName}");
                    var fallbackUsageSnapshot = BuildUsageSnapshot(
                        fallbackResult.Usage,
                        Math.Max(0, (int)Math.Round((DateTime.Now - responseStartedAt).TotalMilliseconds)),
                        roundIndex,
                        responseId);

                    if (!hasStreamedChunk)
                    {
                        await PublishSyntheticChunkEventsAsync(
                            chatTask.Id,
                            agentContext.TemplateDto.Id,
                            agentContext.Agent.Name,
                            responseId,
                            fallbackResult.Output,
                            roundIndex);
                        hasStreamedChunk = true;
                    }

                    var fallbackHistory = await SaveAgentMessageAsync(
                        agentContext,
                        fallbackResult.Output,
                        chatGroup,
                        chatGroupDto,
                        chatTaskDto,
                        chatGroupHistoryService,
                        chatTask,
                        chatTaskService,
                        logger,
                        fallbackUsageSnapshot);

                    PublishMessageEvent(
                        chatTask.Id,
                        fallbackHistory?.Id,
                        agentContext.TemplateDto.Id,
                        agentContext.Agent.Name,
                        responseId,
                        fallbackResult.Output,
                        fallbackUsageSnapshot);

                    return new AgentRunResult
                    {
                        HasOutput = true,
                        OutputText = fallbackResult.Output,
                        ShouldExit = IsExitSignal(fallbackResult.Output)
                    };
                }

                if (!string.IsNullOrWhiteSpace(fallbackResult.Error))
                {
                    logger.AppendLine($"[{chatGroup.Name}] 单智能体 403 回退失败：{fallbackResult.Error}");
                }
            }

            logger.AppendLine($"[{chatGroup.Name}] 单智能体执行异常：{ex.Message}");

            var errorMessage = BuildAgentExecutionFailureMessage(ex);
            var usageSnapshot = BuildUsageSnapshot(
                null,
                Math.Max(0, (int)Math.Round((DateTime.Now - responseStartedAt).TotalMilliseconds)),
                roundIndex,
                responseId);

            if (!hasStreamedChunk)
            {
                await PublishSyntheticChunkEventsAsync(
                    chatTask.Id,
                    agentContext.TemplateDto.Id,
                    agentContext.Agent.Name,
                    responseId,
                    errorMessage,
                    roundIndex);
                hasStreamedChunk = true;
            }

            var history = await SaveAgentMessageAsync(
                agentContext,
                errorMessage,
                chatGroup,
                chatGroupDto,
                chatTaskDto,
                chatGroupHistoryService,
                chatTask,
                chatTaskService,
                logger,
                usageSnapshot);

            PublishMessageEvent(
                chatTask.Id,
                history?.Id,
                agentContext.TemplateDto.Id,
                agentContext.Agent.Name,
                responseId,
                errorMessage,
                usageSnapshot);

            throw;
        }
    }

    private sealed class SingleAgentForbiddenFallbackResult
    {
        public bool Success { get; init; }
        public string Output { get; init; } = string.Empty;
        public UsageDetails Usage { get; init; }
        public string FallbackName { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
    }

    private async Task<SingleAgentForbiddenFallbackResult> TryRunSingleAgentForbiddenFallbackAsync(
        AgentRuntimeContext agentContext,
        string userCommand,
        AIModelService aiModelService,
        string scene)
    {
        var currentSetting = agentContext.Setting ?? (Senparc.AI.Config.SenparcAiSetting as SenparcAiSetting);
        var currentModel = BuildModelDtoFromSetting(currentSetting, "AgentsManager.Current");
        var fallbackErrors = new List<string>();

        if (currentSetting != null)
        {
            try
            {
                SenparcTrace.SendCustomLog(
                    "AgentsManager.AI.StreamFallback",
                    $"{scene} 开始尝试回退（同模型非流式）。Model={BuildModelDiagnosticInfo(currentModel)}");

                var sameModelDirectResult = await RunSingleAgentWithSettingAsync(
                    agentContext,
                    currentSetting,
                    userCommand,
                    $"{agentContext.Template.Name}-direct-fallback");

                if (!string.IsNullOrWhiteSpace(sameModelDirectResult.Output))
                {
                    SenparcTrace.SendCustomLog(
                        "AgentsManager.AI.StreamFallback",
                        $"{scene} 403 后回退成功（同模型非流式）。Model={BuildModelDiagnosticInfo(currentModel)}");

                    return new SingleAgentForbiddenFallbackResult
                    {
                        Success = true,
                        Output = sameModelDirectResult.Output,
                        Usage = sameModelDirectResult.Usage,
                        FallbackName = "SameModelNonStream"
                    };
                }

                fallbackErrors.Add("同模型非流式回退返回空结果");
            }
            catch (Exception sameModelEx)
            {
                fallbackErrors.Add($"同模型非流式回退失败：{FlattenExceptionMessages(sameModelEx)}");
            }
        }

        if (TryBuildAlternateDeploymentModel(currentModel, out var deploymentModel) && deploymentModel != null)
        {
            try
            {
                SenparcTrace.SendCustomLog(
                    "AgentsManager.AI.DeploymentFallback",
                    $"{scene} 开始尝试回退（DeploymentName=ModelId）。Current={BuildModelDiagnosticInfo(currentModel)}; Fallback={BuildModelDiagnosticInfo(deploymentModel)}");

                var deploymentSetting = aiModelService.BuildSenparcAiSetting(deploymentModel);
                var deploymentResult = await RunSingleAgentWithSettingAsync(
                    agentContext,
                    deploymentSetting,
                    userCommand,
                    $"{agentContext.Template.Name}-deployment-fallback");

                if (!string.IsNullOrWhiteSpace(deploymentResult.Output))
                {
                    SenparcTrace.SendCustomLog(
                        "AgentsManager.AI.DeploymentFallback",
                        $"{scene} 403 回退成功（DeploymentName=ModelId）。Current={BuildModelDiagnosticInfo(currentModel)}; Fallback={BuildModelDiagnosticInfo(deploymentModel)}");

                    return new SingleAgentForbiddenFallbackResult
                    {
                        Success = true,
                        Output = deploymentResult.Output,
                        Usage = deploymentResult.Usage,
                        FallbackName = "DeploymentName=ModelId"
                    };
                }

                fallbackErrors.Add("Deployment 回退返回空结果");
            }
            catch (Exception deploymentEx)
            {
                fallbackErrors.Add($"Deployment 回退失败：{FlattenExceptionMessages(deploymentEx)}");
            }
        }

        var defaultSetting = Senparc.AI.Config.SenparcAiSetting as SenparcAiSetting;
        if (defaultSetting != null && CanUseDefaultChatFallback(defaultSetting))
        {
            var defaultModel = BuildModelDtoFromSetting(defaultSetting, "AgentsManager.SystemDefault");
            if (!IsSameChatConfig(currentModel, defaultModel))
            {
                try
                {
                    SenparcTrace.SendCustomLog(
                        "AgentsManager.AI.DefaultFallback",
                        $"{scene} 开始尝试回退（SystemDefault）。Current={BuildModelDiagnosticInfo(currentModel)}; Default={BuildModelDiagnosticInfo(defaultModel)}");

                    var defaultResult = await RunSingleAgentWithSettingAsync(
                        agentContext,
                        defaultSetting,
                        userCommand,
                        $"{agentContext.Template.Name}-default-fallback");

                    if (!string.IsNullOrWhiteSpace(defaultResult.Output))
                    {
                        SenparcTrace.SendCustomLog(
                            "AgentsManager.AI.DefaultFallback",
                            $"{scene} 403 回退成功（SystemDefault）。Current={BuildModelDiagnosticInfo(currentModel)}; Default={BuildModelDiagnosticInfo(defaultModel)}");

                        return new SingleAgentForbiddenFallbackResult
                        {
                            Success = true,
                            Output = defaultResult.Output,
                            Usage = defaultResult.Usage,
                            FallbackName = "SystemDefaultChat"
                        };
                    }

                    fallbackErrors.Add("系统默认模型回退返回空结果");
                }
                catch (Exception defaultEx)
                {
                    fallbackErrors.Add($"系统默认模型回退失败：{FlattenExceptionMessages(defaultEx)}");
                }
            }
        }

        return new SingleAgentForbiddenFallbackResult
        {
            Success = false,
            Error = fallbackErrors.Count == 0 ? "无可用回退方案" : string.Join(" | ", fallbackErrors)
        };
    }

    private static async Task<(string Output, UsageDetails Usage)> RunSingleAgentWithSettingAsync(
        AgentRuntimeContext agentContext,
        SenparcAiSetting setting,
        string userCommand,
        string runnerName)
    {
        var handler = new AgentAiHandler(setting);
        var runner = await handler
            .IWantTo(setting)
            .ConfigChatModel(runnerName, CloneAgentOptions(agentContext.AgentOptions))
            .BuildKernelWithAgentSessionAsync();

        var runResult = await ExecuteRunnerWithSessionRetryAsync(runner, userCommand);
        var output = runResult?.OutputString;
        if (string.IsNullOrWhiteSpace(output) && runResult?.Result != null)
        {
            output = ExtractAgentResponseText(runResult.Result);
        }

        if (ContainsServiceFailureSignature(output))
        {
            throw new NcfExceptionBase(output.Trim());
        }

        return (output ?? string.Empty, runResult?.Result?.Usage);
    }

    private static async Task<SenparcKernelAiResult<string>> ExecuteRunnerWithSessionRetryAsync(
        IWantToRun runner,
        string userCommand,
        Action<AgentResponseUpdate> onUpdate = null)
    {
        var session = runner?.Kernel?.AgentSession;
        if (onUpdate == null)
        {
            try
            {
                return await runner.RunChatAsync(userCommand, session);
            }
            catch when (session != null)
            {
                return await runner.RunChatAsync(userCommand, null);
            }
        }

        try
        {
            return await runner.RunChatAsync(userCommand, session, onUpdate);
        }
        catch when (session != null)
        {
            return await runner.RunChatAsync(userCommand, null, onUpdate);
        }
    }

    private static async Task<ChatGroupHistory> SaveAgentMessageAsync(
        AgentRuntimeContext speakerContext,
        string messageText,
        ChatGroup chatGroup,
        ChatGroupDto chatGroupDto,
        ChatTaskDto chatTaskDto,
        ChatGroupHistoryService chatGroupHistoryService,
        ChatTask chatTask,
        ChatTaskService chatTaskService,
        StringBuilder logger,
        ChatUsageSnapshot usageSnapshot)
    {
        try
        {
            await AgentTemplatePrintMessageMiddleware.SendWechatMessageAsync(
                messageText,
                speakerContext.TemplateDto,
                chatGroupDto,
                chatTaskDto);
        }
        catch (Exception ex)
        {
            SenparcTrace.SendCustomLog("SendWechatMessage 发生异常", ex.Message);
        }

        var usageRemark = ChatUsageRemarkCodec.EncodeMessage(usageSnapshot);

        logger.AppendLine($"[{chatGroup.Name}]组 {speakerContext.Agent.Name} 发送消息：{messageText}");

        var historyDto = new ChatGroupHistoryDto(
            chatGroupDto.Id,
            chatTaskDto.Id,
            null,
            speakerContext.TemplateDto.Id,
            null,
            speakerContext.TemplateDto.Id,
            null,
            messageText,
            MessageType.Text,
            Status.Finished)
        {
            AdminRemark = usageRemark
        };

        var history = await chatGroupHistoryService.CreateHistory(historyDto);
        await chatTaskService.UpdateUsageAggregateAsync(chatTask, usageSnapshot);
        chatTaskDto.TotalPromptTokens += usageSnapshot?.PromptTokens ?? 0;
        chatTaskDto.TotalCompletionTokens += usageSnapshot?.CompletionTokens ?? 0;
        chatTaskDto.TotalTokens += usageSnapshot?.TotalTokens ?? 0;
        chatTaskDto.TotalRounds += 1;

        return history;
    }

    private static ChatUsageSnapshot BuildUsageSnapshot(
        UsageDetails usage,
        int responseMilliseconds,
        int roundIndex,
        string responseId)
    {
        var promptTokens = ClampToInt(usage?.InputTokenCount ?? 0);
        var completionTokens = ClampToInt(usage?.OutputTokenCount ?? 0);
        var totalTokens = ClampToInt(usage?.TotalTokenCount ?? 0);
        if (totalTokens <= 0)
        {
            totalTokens = promptTokens + completionTokens;
        }

        return new ChatUsageSnapshot
        {
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens,
            ResponseMilliseconds = Math.Max(0, responseMilliseconds),
            RoundIndex = Math.Max(0, roundIndex),
            ResponseId = responseId
        };
    }

    private static int ClampToInt(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    private static string ExtractAgentResponseText(AgentResponse response)
    {
        if (response == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            return response.Text;
        }

        if (response.Messages == null || response.Messages.Count == 0)
        {
            return string.Empty;
        }

        var messageTexts = response.Messages
            .Select(ExtractChatMessageText)
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .ToList();

        return messageTexts.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, messageTexts);
    }

    private static string ExtractChatMessageText(ChatMessage chatMessage)
    {
        if (chatMessage == null)
        {
            return string.Empty;
        }

        var textSegments = chatMessage.Contents?
            .OfType<TextContent>()
            .Select(z => z.Text)
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .ToList();

        if (textSegments?.Count > 0)
        {
            return string.Join(Environment.NewLine, textSegments);
        }

        return chatMessage.ToString() ?? string.Empty;
    }

    private static string ExtractAgentResponseUpdateText(AgentResponseUpdate update)
    {
        if (update == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(update.Text))
        {
            return update.Text;
        }

        var textSegments = update.Contents?
            .OfType<TextContent>()
            .Select(z => z.Text)
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .ToList();

        if (textSegments?.Count > 0)
        {
            return string.Join(Environment.NewLine, textSegments);
        }

        // 只认真实文本内容，避免把 ToString() 结果误判为“已流式输出”，
        // 从而错过后续 synthetic chunk 回退。
        return string.Empty;
    }

    private async Task PublishSyntheticChunkEventsAsync(
        int chatTaskId,
        int? fromAgentTemplateId,
        string fromAgentName,
        string responseId,
        string text,
        int roundIndex)
    {
        if (string.IsNullOrWhiteSpace(responseId) || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var chunkList = SplitStreamText(text).ToList();
        if (chunkList.Count == 0)
        {
            return;
        }

        for (var i = 0; i < chunkList.Count; i++)
        {
            PublishChunkEvent(
                chatTaskId,
                fromAgentTemplateId,
                fromAgentName,
                responseId,
                chunkList[i],
                roundIndex);

            if (i < chunkList.Count - 1)
            {
                // 适当拉开 synthetic chunk 的间隔，避免前端视觉上“整段瞬出”。
                await Task.Delay(28);
            }
        }
    }

    private static IEnumerable<string> SplitStreamText(string text, int maxChunkLength = 24)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var buffer = new StringBuilder();
        foreach (var ch in text)
        {
            buffer.Append(ch);
            if (ShouldBreakStreamChunk(ch, buffer.Length, maxChunkLength))
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }

    private static bool ShouldBreakStreamChunk(char ch, int currentLength, int maxChunkLength)
    {
        if (currentLength >= maxChunkLength)
        {
            return true;
        }

        return ch is '\n' or '。' or '！' or '？' or '!' or '?' or ';' or '；' or '，' or ',';
    }

    private static string BuildAgentExecutionFailureMessage(Exception ex)
    {
        var raw = ex?.Message ?? "未知错误";
        var normalized = raw
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        if (normalized.Length > 240)
        {
            normalized = normalized[..240];
        }

        if (normalized.Contains("Status: 403", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
        {
            return "系统提示：AI 服务返回 403（Forbidden），当前任务无法继续。请检查模型权限、API Key 或所选模型可用性。";
        }

        if (normalized.Contains("Status: 401", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return "系统提示：AI 服务认证失败（401），请检查 API Key / Endpoint 配置。";
        }

        return $"系统提示：AI 服务调用失败，任务已中断。原因：{normalized}";
    }

    private static ChatClientAgentOptions CloneAgentOptions(ChatClientAgentOptions options)
    {
        if (options == null)
        {
            return new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions()
            };
        }

        return new ChatClientAgentOptions
        {
            Name = options.Name,
            Description = options.Description,
            ChatOptions = CloneChatOptions(options.ChatOptions)
        };
    }

    private static ChatOptions CloneChatOptions(ChatOptions chatOptions)
    {
        if (chatOptions == null)
        {
            return new ChatOptions();
        }

        return new ChatOptions
        {
            Instructions = chatOptions.Instructions,
            MaxOutputTokens = chatOptions.MaxOutputTokens,
            Temperature = chatOptions.Temperature,
            TopP = chatOptions.TopP,
            FrequencyPenalty = chatOptions.FrequencyPenalty,
            PresencePenalty = chatOptions.PresencePenalty,
            AllowMultipleToolCalls = chatOptions.AllowMultipleToolCalls,
            StopSequences = chatOptions.StopSequences?.ToList() ?? new List<string>(),
            Tools = chatOptions.Tools?.ToList()
        };
    }

    private static bool ContainsForbiddenStatus(Exception ex)
    {
        if (ex == null)
        {
            return false;
        }

        var chain = FlattenExceptionMessages(ex);
        if (chain.Contains("Status: 403 (Forbidden)", StringComparison.OrdinalIgnoreCase)
            || chain.Contains("StatusCode: 403", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var has403 = chain.Contains("403", StringComparison.OrdinalIgnoreCase);
        var hasForbidden = chain.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                           || chain.Contains("禁止", StringComparison.OrdinalIgnoreCase)
                           || chain.Contains("拒绝", StringComparison.OrdinalIgnoreCase);

        return has403 && hasForbidden;
    }

    private static bool ContainsServiceFailureSignature(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        var has403 = normalized.Contains("Status: 403", StringComparison.OrdinalIgnoreCase)
                     || normalized.Contains("StatusCode: 403", StringComparison.OrdinalIgnoreCase);
        var has401 = normalized.Contains("Status: 401", StringComparison.OrdinalIgnoreCase)
                     || normalized.Contains("StatusCode: 401", StringComparison.OrdinalIgnoreCase);
        var hasServiceFailed = normalized.Contains("Service request failed", StringComparison.OrdinalIgnoreCase)
                               || normalized.Contains("ClientResultException", StringComparison.OrdinalIgnoreCase);

        if (hasServiceFailed && (has403 || has401))
        {
            return true;
        }

        return normalized.StartsWith("Status: 403", StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith("Status: 401", StringComparison.OrdinalIgnoreCase);
    }

    private static string FlattenExceptionMessages(Exception ex)
    {
        if (ex == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var current = ex;
        var depth = 0;
        while (current != null && depth < 8)
        {
            if (depth > 0)
            {
                sb.Append(" -> ");
            }

            sb.Append('[');
            sb.Append(current.GetType().Name);
            sb.Append("] ");
            sb.Append(current.Message?.Trim());

            current = current.InnerException;
            depth++;
        }

        return sb.ToString();
    }

    private static bool CanUseDefaultChatFallback(SenparcAiSetting? defaultSetting)
    {
        if (defaultSetting == null || defaultSetting.AiPlatform == AiPlatform.UnSet)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(defaultSetting.ModelName?.Chat))
        {
            return false;
        }

        if (defaultSetting.AiPlatform != AiPlatform.Ollama && string.IsNullOrWhiteSpace(defaultSetting.ApiKey))
        {
            return false;
        }

        return defaultSetting.AiPlatform switch
        {
            AiPlatform.OpenAI => true,
            AiPlatform.Ollama => !string.IsNullOrWhiteSpace(defaultSetting.OllamaEndpoint),
            _ => !string.IsNullOrWhiteSpace(defaultSetting.Endpoint)
        };
    }

    private static AIModelDto? BuildModelDtoFromSetting(SenparcAiSetting? setting, string alias)
    {
        if (setting == null)
        {
            return null;
        }

        var apiVersion = setting.AiPlatform switch
        {
            AiPlatform.AzureOpenAI => setting.AzureOpenAIApiVersion,
            AiPlatform.NeuCharAI => setting.NeuCharAIApiVersion,
            _ => null
        };

        return new AIModelDto
        {
            Id = 0,
            Alias = alias,
            AiPlatform = setting.AiPlatform,
            ConfigModelType = ConfigModelType.Chat,
            ModelId = setting.ModelName?.Chat,
            DeploymentName = setting.DeploymentName ?? setting.ModelName?.Chat,
            Endpoint = setting.Endpoint,
            ApiKey = setting.ApiKey,
            ApiVersion = apiVersion
        };
    }

    private static bool IsSameChatConfig(AIModelDto? left, AIModelDto? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.AiPlatform == right.AiPlatform
               && string.Equals(
                   NormalizeEndpointForDiagnostics(left.AiPlatform, left.Endpoint),
                   NormalizeEndpointForDiagnostics(right.AiPlatform, right.Endpoint),
                   StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.ModelId, right.ModelId, StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.DeploymentName, right.DeploymentName, StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.ApiKey, right.ApiKey, StringComparison.Ordinal);
    }

    private static bool TryBuildAlternateDeploymentModel(AIModelDto? model, out AIModelDto? fallbackModel)
    {
        fallbackModel = null;
        if (model == null)
        {
            return false;
        }

        if (model.AiPlatform != AiPlatform.AzureOpenAI && model.AiPlatform != AiPlatform.NeuCharAI)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(model.ModelId))
        {
            return false;
        }

        if (string.Equals(model.DeploymentName, model.ModelId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fallbackModel = new AIModelDto
        {
            Id = model.Id,
            Alias = $"{model.Alias ?? "Model"}_DeploymentAsModelId",
            DeploymentName = model.ModelId,
            ModelId = model.ModelId,
            Endpoint = model.Endpoint,
            AiPlatform = model.AiPlatform,
            ConfigModelType = model.ConfigModelType,
            OrganizationId = model.OrganizationId,
            ApiKey = model.ApiKey,
            ApiVersion = model.ApiVersion,
            Note = model.Note,
            MaxToken = model.MaxToken,
            IsShared = model.IsShared,
            Show = model.Show
        };
        return true;
    }

    private static string BuildModelDiagnosticInfo(AIModelDto? model)
    {
        if (model == null)
        {
            return "模型配置为空（AIModelDto == null）";
        }

        var endpoint = NormalizeEndpointForDiagnostics(model.AiPlatform, model.Endpoint);
        var apiKeyStatus = string.IsNullOrWhiteSpace(model.ApiKey)
            ? "empty"
            : $"set(len:{model.ApiKey.Length})";

        return $"AIModelDbId={model.Id}, ConfigType={model.ConfigModelType}, ModelId={model.ModelId ?? "(null)"}, Alias={model.Alias ?? "(null)"}, Platform={model.AiPlatform}, Deployment={model.DeploymentName ?? "(null)"}, Endpoint={endpoint ?? "(null)"}, ApiVersion={model.ApiVersion ?? "(null)"}, ApiKey={apiKeyStatus}";
    }

    private static string NormalizeEndpointForDiagnostics(AiPlatform platform, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return endpoint;
        }

        var normalized = endpoint.Trim();
        if (platform == AiPlatform.NeuCharAI && !normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized += "/";
        }

        return normalized;
    }

    private void PublishChunkEvent(
        int chatTaskId,
        int? fromAgentTemplateId,
        string fromAgentName,
        string responseId,
        string text,
        int roundIndex)
    {
        _chatTaskStreamHub.Publish(new ChatTaskStreamEvent
        {
            EventType = "chunk",
            ChatTaskId = chatTaskId,
            FromAgentTemplateId = fromAgentTemplateId,
            FromAgentName = fromAgentName,
            ResponseId = responseId,
            Text = text,
            IsFinal = false,
            RoundIndex = roundIndex,
            Timestamp = DateTimeOffset.Now
        });
    }

    private void PublishMessageEvent(
        int chatTaskId,
        int? historyId,
        int? fromAgentTemplateId,
        string fromAgentName,
        string responseId,
        string message,
        ChatUsageSnapshot usageSnapshot)
    {
        _chatTaskStreamHub.Publish(new ChatTaskStreamEvent
        {
            EventType = "message",
            ChatTaskId = chatTaskId,
            HistoryId = historyId,
            FromAgentTemplateId = fromAgentTemplateId,
            FromAgentName = fromAgentName,
            ResponseId = responseId,
            Text = message,
            IsFinal = true,
            PromptTokens = usageSnapshot?.PromptTokens ?? 0,
            CompletionTokens = usageSnapshot?.CompletionTokens ?? 0,
            TotalTokens = usageSnapshot?.TotalTokens ?? 0,
            ResponseMilliseconds = usageSnapshot?.ResponseMilliseconds ?? 0,
            RoundIndex = usageSnapshot?.RoundIndex ?? 0,
            Timestamp = DateTimeOffset.Now
        });
    }

    private void PublishStatusEvent(int chatTaskId, ChatTask_Status status)
    {
        _chatTaskStreamHub.Publish(new ChatTaskStreamEvent
        {
            EventType = "status",
            ChatTaskId = chatTaskId,
            Text = status.ToString(),
            IsFinal = status == ChatTask_Status.Finished
                      || status == ChatTask_Status.Cancelled,
            Timestamp = DateTimeOffset.Now
        });
    }

    private static async Task<(string Prompt, ISenparcAiSetting Setting)> ResolvePromptAndSettingAsync(
        PromptItemService promptItemService,
        AgentTemplateDto templateDto,
        AgentTemplate template,
        AIModelService aiModelService,
        ISenparcAiSetting defaultSetting)
    {
        var promptText = template.SystemMessage;
        var currentSetting = defaultSetting;

        if (!templateDto.PromptCode.IsNullOrEmpty() && PromptItem.IsPromptVersion(templateDto.PromptCode))
        {
            var promptResult = await promptItemService.GetWithVersionAsync(templateDto.PromptCode, isAvg: true);
            if (promptResult?.PromptItem != null)
            {
                promptText = promptResult.PromptItem.Content;
                currentSetting = promptResult.SenparcAiSetting ?? defaultSetting;

                if (promptResult.PromptItem.AIModelDto != null)
                {
                    try
                    {
                        var availableModelResult = await aiModelService.GetValiableChatModel(promptResult.PromptItem.AIModelDto);
                        currentSetting = availableModelResult.AiSetting ?? currentSetting;
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog(
                            "AgentsManager.ResolvePromptSetting",
                            $"获取可用 Chat 模型失败，继续使用原始设置：{ex.GetType().Name} {ex.Message}");
                    }
                }
            }
        }
        else if (!templateDto.PromptCode.IsNullOrEmpty())
        {
            promptText = templateDto.PromptCode;
        }

        if (promptText.IsNullOrEmpty())
        {
            promptText = "你是一个有帮助的智能体。";
        }

        return (promptText, currentSetting);
    }

    private static async Task<List<AITool>> BuildAgentToolsAsync(
        IServiceProvider services,
        AIPluginHub aiPlugins,
        AgentAiHandler agentHandler,
        AgentTemplateDto templateDto,
        int templateId)
    {
        var tools = new List<AITool>();

        // 1. 内置 Function-calling 插件
        var functionCallNames = templateDto.FunctionCallNames.IsNullOrEmpty()
            ? Array.Empty<string>()
            : templateDto.FunctionCallNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        foreach (var functionCall in functionCallNames)
        {
            try
            {
                var functionCallType = aiPlugins.GetPluginType(functionCall, true);
                if (functionCallType == null)
                {
                    SenparcTrace.SendCustomLog("导入 Plugin 失败", $"FunctionCall 名称不存在：{functionCall}");
                    continue;
                }

                var plugin = services.GetService(functionCallType);
                if (plugin == null)
                {
                    SenparcTrace.SendCustomLog("导入 Plugin 失败", $"未注册 Plugin 类型：{functionCallType.FullName}");
                    continue;
                }

                var pluginTools = agentHandler.GetAITools(plugin);
                if (pluginTools.Count > 0)
                {
                    tools.AddRange(pluginTools);
                }
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("导入 Plugin 失败", ex.Message);
            }
        }

        // 2. MCP tools（McpClientTool 继承 AIFunction，可直接并入）
        var mcpTools = await BuildMcpToolsAsync(templateDto.McpEndpoints, templateId);
        if (mcpTools.Count > 0)
        {
            tools.AddRange(mcpTools);
        }

        return tools
            .GroupBy(z => z.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static async Task<List<AITool>> BuildMcpToolsAsync(string mcpEndpointsJson, int templateId)
    {
        var toolList = new List<AITool>();

        if (string.IsNullOrWhiteSpace(mcpEndpointsJson))
        {
            return toolList;
        }

        try
        {
            var endpoints = JsonSerializer.Deserialize<Dictionary<string, McpEndpoint>>(mcpEndpointsJson)
                           ?? new Dictionary<string, McpEndpoint>();

            foreach (var endpoint in endpoints)
            {
                var mcpName = endpoint.Key;
                var endpointUrl = endpoint.Value?.url;

                if (string.IsNullOrWhiteSpace(mcpName) || string.IsNullOrWhiteSpace(endpointUrl))
                {
                    continue;
                }

                try
                {
                    //var transport = new SseClientTransport(new SseClientTransportOptions
                    //{
                    //    Endpoint = new Uri(endpointUrl),
                    //    Name = mcpName
                    //});

                    //var client = await McpClientFactory.CreateAsync(transport);
                    //var tools = await client.ListToolsAsync();

                    //foreach (var tool in tools)
                    //{
                    //    Console.WriteLine($"Agent: {templateId} MCP: {mcpName} : {tool.Name} ({tool.Description})");
                    //    toolList.Add(tool);
                    //}

                    var hostedMcpServerTool = new HostedMcpServerTool(mcpName, endpointUrl)
                    {
                        // AllowedTools = ["microsoft_docs_search"],
                        ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire
                    };
                    toolList.Add(hostedMcpServerTool);
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                }
            }
        }
        catch (Exception ex)
        {
            SenparcTrace.SendCustomLog("解析 MCP Endpoints 失败", ex.Message);
        }

        return toolList;
    }
}
