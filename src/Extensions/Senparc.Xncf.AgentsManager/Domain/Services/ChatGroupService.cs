using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
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
        TaskList.Add(task);
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
                    .IWantTo()
                    .ConfigChatModel($"{template.Name}-{member.Uid}", agentOptions)
                    .BuildKernelWithAgentSessionAsync();

                runtimeContexts.Add(new AgentRuntimeContext
                {
                    Template = template,
                    TemplateDto = templateDto,
                    Agent = runner.Kernel.ChatClientAgent,
                    Runner = runner
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
                await RunSingleAgentAsync(
                    enterContext,
                    userCommand,
                    logger,
                    chatGroup,
                    chatGroupDto,
                    chatTaskDto,
                    chatGroupHistoryService,
                    runningKey,
                    chatTask,
                    chatTaskService,
                    1);

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

            var participantContexts = runtimeContexts
                .Where(z => z.Template.Id != adminContext.Template.Id)
                .ToList();

            participantContexts = participantContexts
                .OrderByDescending(z => z.Template.Id == enterContext.Template.Id)
                .ToList();

            if (participantContexts.Count == 0)
            {
                throw new NcfExceptionBase($"聊天组【{chatGroup.Name}】未找到可参与协作的成员智能体。");
            }

            #region Microsoft Agent Framework 多智能体工作流

#pragma warning disable MAAIW001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            Workflow workflow = new MagenticWorkflowBuilder(adminContext.Agent)
                .AddParticipants(participantContexts.Select(z => z.Agent).ToList())
                .WithName(chatGroup.Name)
                .WithDescription(string.IsNullOrWhiteSpace(chatGroup.Description) ? "AgentsManager ChatGroup Workflow" : chatGroup.Description)
                .RequirePlanSignoff(false)
                .WithMaxRounds(request.ChatMaxRound > 0 ? request.ChatMaxRound : ChatMaxRound)
                .WithMaxStalls(3)
                .WithMaxResets(2)
                .Build();

            var taskPrompt = BuildWorkflowPrompt(enterContext.Agent.Name, participantContexts.Select(z => z.Agent.Name), userCommand);

            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
                workflow,
                new List<ChatMessage> { new(ChatRole.User, taskPrompt) });

            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            var contextByAgentName = runtimeContexts
                .GroupBy(z => z.Agent.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            string activeResponseKey = null;
            string activeExecutorId = null;
            var activeResponseText = new StringBuilder();
            UsageDetails activeUsageDetails = null;
            DateTime? activeResponseStartedAt = null;
            var roundIndex = 0;
            var shouldExit = false;

            async Task FlushActiveResponseAsync()
            {
                var messageText = activeResponseText.ToString().Trim();
                if (messageText.Length == 0 || string.IsNullOrWhiteSpace(activeExecutorId))
                {
                    activeResponseText.Clear();
                    activeResponseKey = null;
                    activeExecutorId = null;
                    activeUsageDetails = null;
                    activeResponseStartedAt = null;
                    return;
                }

                roundIndex += 1;
                var responseMilliseconds = activeResponseStartedAt.HasValue
                    ? Math.Max(0, (int)Math.Round((DateTime.Now - activeResponseStartedAt.Value).TotalMilliseconds))
                    : 0;

                var usageSnapshot = BuildUsageSnapshot(activeUsageDetails, responseMilliseconds, roundIndex, activeResponseKey);

                if (contextByAgentName.TryGetValue(activeExecutorId, out var speakerContext))
                {
                    var history = await SaveAgentMessageAsync(
                        speakerContext,
                        messageText,
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
                        activeResponseKey,
                        messageText,
                        usageSnapshot);
                }
                else
                {
                    logger.AppendLine($"[{chatGroup.Name}]组 {activeExecutorId} 发送消息：{messageText}");
                }

                if (messageText.Contains("exit", StringComparison.OrdinalIgnoreCase))
                {
                    shouldExit = true;
                }

                activeResponseText.Clear();
                activeResponseKey = null;
                activeExecutorId = null;
                activeUsageDetails = null;
                activeResponseStartedAt = null;
            }

            await foreach (var workflowEvent in run.WatchStreamAsync())
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

                switch (workflowEvent)
                {
                    case AgentResponseUpdateEvent updateEvent:
                    {
                        var responseId = updateEvent.Update.ResponseId
                                         ?? updateEvent.Update.MessageId
                                         ?? updateEvent.ExecutorId;

                        if (!string.Equals(activeResponseKey, responseId, StringComparison.Ordinal))
                        {
                            await FlushActiveResponseAsync();
                            activeResponseKey = responseId;
                            activeExecutorId = updateEvent.ExecutorId;
                            activeUsageDetails = null;
                            activeResponseStartedAt = DateTime.Now;
                        }

                        if (!string.IsNullOrEmpty(updateEvent.Update.Text))
                        {
                            activeResponseText.Append(updateEvent.Update.Text);

                            if (contextByAgentName.TryGetValue(updateEvent.ExecutorId, out var streamContext))
                            {
                                PublishChunkEvent(
                                    chatTask.Id,
                                    streamContext.TemplateDto.Id,
                                    streamContext.Agent.Name,
                                    responseId,
                                    updateEvent.Update.Text,
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
                    case WorkflowErrorEvent workflowError:
                        throw workflowError.Exception ?? new Exception("Workflow execution failed.");
                    case ExecutorFailedEvent executorFailed:
                        throw new Exception($"Executor '{executorFailed.ExecutorId}' failed: {executorFailed.Data}");
                }

                if (shouldExit)
                {
                    break;
                }
            }

            await FlushActiveResponseAsync();
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

    private async Task RunSingleAgentAsync(
        AgentRuntimeContext agentContext,
        string userCommand,
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
            return;
        }

        UsageDetails streamUsageDetails = null;
        var responseId = Guid.NewGuid().ToString("n");
        var responseStartedAt = DateTime.Now;

        var result = await agentContext.Runner.RunChatAsync(
            userCommand,
            agentContext.Runner.Kernel.AgentSession,
            update =>
            {
                if (!string.IsNullOrEmpty(update?.Text))
                {
                    PublishChunkEvent(
                        chatTask.Id,
                        agentContext.TemplateDto.Id,
                        agentContext.Agent.Name,
                        responseId,
                        update.Text,
                        roundIndex);
                }

                if (update?.Contents?.FirstOrDefault(z => z is UsageContent) is UsageContent usageContent
                    && usageContent.Details != null)
                {
                    streamUsageDetails = usageContent.Details;
                }
            });

        var output = result?.OutputString;

        if (!string.IsNullOrWhiteSpace(output))
        {
            var usageSnapshot = BuildUsageSnapshot(
                streamUsageDetails ?? result?.Result?.Usage,
                Math.Max(0, (int)Math.Round((DateTime.Now - responseStartedAt).TotalMilliseconds)),
                roundIndex,
                responseId);

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
        ISenparcAiSetting defaultSetting)
    {
        var promptText = template.SystemMessage;
        var currentSetting = defaultSetting;

        if (!templateDto.PromptCode.IsNullOrEmpty() && PromptItem.IsPromptVersion(templateDto.PromptCode))
        {
            var promptResult = await promptItemService.GetWithVersionAsync(template.PromptCode, isAvg: true);
            if (promptResult?.PromptItem != null)
            {
                promptText = promptResult.PromptItem.Content;
                currentSetting = promptResult.SenparcAiSetting ?? defaultSetting;
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

    private static async Task<List<AIFunction>> BuildAgentToolsAsync(
        IServiceProvider services,
        AIPluginHub aiPlugins,
        AgentAiHandler agentHandler,
        AgentTemplateDto templateDto,
        int templateId)
    {
        var tools = new List<AIFunction>();

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

    private static async Task<List<AIFunction>> BuildMcpToolsAsync(string mcpEndpointsJson, int templateId)
    {
        var toolList = new List<AIFunction>();

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
                    var transport = new SseClientTransport(new SseClientTransportOptions
                    {
                        Endpoint = new Uri(endpointUrl),
                        Name = mcpName
                    });

                    var client = await McpClientFactory.CreateAsync(transport);
                    var tools = await client.ListToolsAsync();

                    foreach (var tool in tools)
                    {
                        Console.WriteLine($"Agent: {templateId} MCP: {mcpName} : {tool.Name} ({tool.Description})");
                        toolList.Add(tool);
                    }
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
