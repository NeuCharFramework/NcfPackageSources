using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System.Linq; 
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Service;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class PromptOptimizationService
    {
        private readonly IEventBus _eventBus;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly ChatGroupService _chatGroupService;
        private readonly ChatGroupMemberService _chatGroupMemberService;
        private readonly ILogger<PromptOptimizationService> _logger;
        
        // Used to store pending requests: RequestId -> TCS
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<PromptOptimizationResponseEvent>> _pendingRequests 
            = new ConcurrentDictionary<string, TaskCompletionSource<PromptOptimizationResponseEvent>>();
            
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<PromptInitResponseEvent>> _pendingInitRequests 
            = new ConcurrentDictionary<string, TaskCompletionSource<PromptInitResponseEvent>>();

        /// <summary>
        /// Two concurrent HTTP optimizations are prohibited under the same target + demand + model (double-clicking will send two RequestIds, each with a row inserted and the content is always the same).
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _optimizeInFlightByPrompt = new();

        private static string BuildOptimizeSingleFlightKey(string promptCode, string userRequirement, OptimizationContext context)
        {
            var req = userRequirement?.Trim() ?? string.Empty;
            var mid = context?.ModelId ?? 0;
            return $"{promptCode?.Trim() ?? ""}\u001F{req}\u001F{mid}";
        }

        public PromptOptimizationService(
            IEventBus eventBus, 
            AgentsTemplateService agentsTemplateService,
            ChatGroupService chatGroupService,
            ChatGroupMemberService chatGroupMemberService,
            ILogger<PromptOptimizationService> logger)
        {
            _eventBus = eventBus;
            _agentsTemplateService = agentsTemplateService;
            _chatGroupService = chatGroupService;
            _chatGroupMemberService = chatGroupMemberService;
            _logger = logger;
        }

        /// <summary>
        /// Make sure the PromptCatalyzer Agent and related resources are initialized (fine-grained check)
        /// </summary>
        /// <param name="modelId">Optional: user-specified AI Model ID</param>
        public async Task<PromptInitResponseEvent> EnsureInitializedAsync(int? modelId = null)
        {
            _logger.LogInformation("========== EnsureInitializedAsync 开始（细粒度检查）==========");
            _logger.LogInformation("请求的 ModelId: {ModelId}", modelId);

            // === Step 1: Check if the Agent already exists ===
            _logger.LogInformation("【步骤1/3】检查 PromptCatalyzer Agent 是否已存在...");
            var agent = _agentsTemplateService.GetObject(z => z.Name == "PromptCatalyzer");
            
            string promptCode = null;
            bool agentCreated = false;
            
            if (agent != null)
            {
                _logger.LogInformation("  ✅ Agent 已存在，ID: {AgentId}, PromptCode: {PromptCode}", 
                    agent.Id, agent.SystemMessage);
                promptCode = agent.SystemMessage;  // Agent's SystemMessage stores PromptCode

                const string pluginTypeName = "Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins.PromptOptimizationPlugin";
                if (string.IsNullOrWhiteSpace(agent.FunctionCallNames))
                {
                    var dto = _agentsTemplateService.Mapper.Map<AgentTemplateDto>(agent);
                    dto.FunctionCallNames = pluginTypeName;
                    agent.UpdateFromDto(dto);
                    await _agentsTemplateService.SaveObjectAsync(agent);
                    _logger.LogInformation("  🔧 已补全已有 Agent 的 FunctionCallNames（PromptOptimizationPlugin）");
                }
            }
            else
            {
                // Agent does not exist and needs to be created
                _logger.LogInformation("  Agent 不存在，开始创建流程...");
                
                // Create PromptItem via EventBus request
                var requestId = Guid.NewGuid().ToString();
                var tcs = new TaskCompletionSource<PromptInitResponseEvent>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                
                if (!_pendingInitRequests.TryAdd(requestId, tcs))
                {
                    throw new NcfExceptionBase("Failed to register init request ID.");
                }
                
                try
                {
                    _logger.LogInformation("  发布 PromptInitRequestEvent: RequestId={RequestId}, ModelId={ModelId}", 
                        requestId, modelId);
                    
                    var requestEvent = new PromptInitRequestEvent(requestId, modelId);
                    await _eventBus.PublishAsync(requestEvent);
                    
                    _logger.LogInformation("  等待 PromptInitResponseEvent（最长2分钟）...");
                    
                    var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        _pendingInitRequests.TryRemove(requestId, out _);
                        _logger.LogError("  ❌ 等待 PromptInitResponseEvent 超时（2分钟）");
                        throw new TimeoutException("PromptCatalyzer 初始化超时（2 分钟）");
                    }
                    
                    var response = await tcs.Task;
                    _logger.LogInformation("  ✅ 收到 PromptInitResponseEvent: Success={Success}, PromptCode={PromptCode}", 
                        response.Success, response.PromptCode);
                    
                    if (!response.Success)
                    {
                        throw new Exception($"Prompt 初始化失败: {response.ErrorMessage}");
                    }
                    
                    if (string.IsNullOrEmpty(response.PromptCode))
                    {
                        throw new Exception("Prompt 初始化返回的 PromptCode 为空");
                    }
                    
                    promptCode = response.PromptCode;
                }
                catch (Exception ex)
                {
                    _pendingInitRequests.TryRemove(requestId, out _);
                    _logger.LogError(ex, "❌ EventBus 初始化失败");
                    throw;
                }
                
                // Create Agent
                _logger.LogInformation("【步骤2/3】创建 PromptCatalyzer Agent...");
                _logger.LogInformation("  PromptCode: {PromptCode}", promptCode);
                
                var newAgent = new AgentTemplate(
                    name: "PromptCatalyzer",
                    systemMessage: promptCode,
                    enable: true,
                    description: "自动优化 Prompt 内容和参数（Temperature 等）的 AI Agent",
                    promptCode: promptCode,
                    hookRobotType: HookRobotType.None,
                    hookRobotParameter: null,
                    avastar: null,
                    functionCallNames: "Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins.PromptOptimizationPlugin",  // 🔥 Key: Set function-calling
                    mcpEndpoints: null
                );
                
                await _agentsTemplateService.SaveObjectAsync(newAgent);
                _logger.LogInformation("  ✅ Agent 创建成功！AgentId: {AgentId}, FunctionCallNames: {FunctionCallNames}", 
                    newAgent.Id, newAgent.FunctionCallNames);
                
                agent = newAgent;
                agentCreated = true;
            }
            
            // === Step 3: Check if the ChatGroup already exists (independent check) ===
            _logger.LogInformation("【步骤3/3】检查 ChatGroup 是否已存在...");
            var chatGroup = await _chatGroupService.GetObjectAsync(z => z.Name == "PromptCatalyzer-OptimizationGroup");
            
            if (chatGroup != null)
            {
                _logger.LogInformation("  ✅ ChatGroup 已存在，ID: {GroupId}, Admin={AdminId}, Enter={EnterId}", 
                    chatGroup.Id, chatGroup.AdminAgentTemplateId, chatGroup.EnterAgentTemplateId);
                
                // Verify that the Agent reference for the ChatGroup is correct
                if (chatGroup.AdminAgentTemplateId != agent.Id || chatGroup.EnterAgentTemplateId != agent.Id)
                {
                    _logger.LogWarning("  ⚠️  ChatGroup 的 Agent 引用不正确，需要修复");
                    _logger.LogWarning("    当前 Admin={Current}, 应为 {Expected}", 
                        chatGroup.AdminAgentTemplateId, agent.Id);
                    _logger.LogWarning("    当前 Enter={Current}, 应为 {Expected}", 
                        chatGroup.EnterAgentTemplateId, agent.Id);
                    
                    // TODO: Here you can choose to update the Agent reference of ChatGroup, or prompt the user to handle it manually.
                }
            }
            else
            {
                _logger.LogInformation("  ChatGroup 不存在，开始创建...");
                _logger.LogInformation("  使用 Agent ID: {AgentId}", agent.Id);
                
                chatGroup = new ChatGroup(
                    name: "PromptCatalyzer-OptimizationGroup",
                    enable: true,
                    state: ChatGroupState.Running,
                    description: "PromptCatalyzer 专用优化群组，用于执行 Prompt 优化任务",
                    adminAgentTemplateId: agent.Id,
                    enterAgentTemplateId: agent.Id
                );
                
                await _chatGroupService.SaveObjectAsync(chatGroup);
                _logger.LogInformation("  ✅ ChatGroup 创建成功！GroupId: {GroupId}, Name: {Name}", 
                    chatGroup.Id, chatGroup.Name);
            }
            
            _logger.LogInformation("========== EnsureInitializedAsync 完成 ==========");
            _logger.LogInformation("  最终状态：Agent={AgentId}, ChatGroup={GroupId}, Agent是否新创建={AgentCreated}", 
                agent.Id, chatGroup.Id, agentCreated);
            
            return new PromptInitResponseEvent(
                Guid.Empty.ToString(), 
                promptCode ?? agent.SystemMessage,
                true, 
                "Initialized successfully");
        }

        /// <summary>
        /// Complete the initialization request (called by PromptInitResponseHandler)
        /// </summary>
        public void CompleteInitRequest(string requestId, PromptInitResponseEvent response)
        {
            if (_pendingInitRequests.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(response);
                _logger.LogDebug("Completed init request: {RequestId}", requestId);
            }
            else
            {
                _logger.LogWarning("Received PromptInitResponse for unknown RequestId: {RequestId}", requestId);
            }
        }

        /// <summary>
        /// Optimize the specified Prompt (including content and parameters)
        /// </summary>
        public async Task<PromptOptimizationResponseEvent> OptimizePromptAsync(
            string promptCode, 
            string promptContent,
            string userRequirement,
            OptimizationContext context)
        {
            // 1. Make sure the Agent is initialized
            await EnsureInitializedAsync();

            var flightKey = BuildOptimizeSingleFlightKey(promptCode, userRequirement, context);
            var flightSem = _optimizeInFlightByPrompt.GetOrAdd(flightKey, _ => new SemaphoreSlim(1, 1));
            if (!await flightSem.WaitAsync(TimeSpan.Zero).ConfigureAwait(false))
            {
                _logger.LogWarning(
                    "OptimizePromptAsync rejected: another optimization in flight for key={FlightKey}",
                    flightKey);
                throw new InvalidOperationException("该 Prompt 已有优化任务正在执行，请勿重复提交（例如避免双击）。");
            }

            try
            {
                var requestId = Guid.NewGuid().ToString();
                var tcs = new TaskCompletionSource<PromptOptimizationResponseEvent>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                if (!_pendingRequests.TryAdd(requestId, tcs))
                {
                    throw new InvalidOperationException("Failed to register request ID.");
                }

                try
                {
                    _logger.LogInformation(
                        "Publishing PromptOptimizationRequestEvent: RequestId={RequestId}, PromptCode={PromptCode}",
                        requestId, promptCode);

                    // 2. Publish optimization request events
                    var requestEvent = new PromptOptimizationRequestEvent(
                        requestId, 
                        promptCode, 
                        promptContent,
                        userRequirement,
                        context);
                    await _eventBus.PublishAsync(requestEvent);

                    // 3. Waiting for response (Agent multi-round dialogue may be slower)
                    var timeoutTask = Task.Delay(TimeSpan.FromMinutes(15));
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        _pendingRequests.TryRemove(requestId, out _);
                        throw new TimeoutException("Prompt 优化请求超时（15 分钟）");
                    }

                    var response = await tcs.Task;
                    _logger.LogInformation(
                        "Received PromptOptimizationResponse: RequestId={RequestId}, NewPromptCode={NewPromptCode}, Score={Score}",
                        requestId, response.NewPromptCode, response.Score);
                    return response;
                }
                catch (Exception ex)
                {
                    _pendingRequests.TryRemove(requestId, out _);
                    _logger.LogError(ex, "Failed to optimize Prompt: {PromptCode}", promptCode);
                    throw;
                }
            }
            finally
            {
                try
                {
                    flightSem.Release();
                }
                catch (SemaphoreFullException)
                {
                    // ignore
                }

                if (_optimizeInFlightByPrompt.TryRemove(flightKey, out var removed) && removed != null)
                {
                    try
                    {
                        removed.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        /// <summary>
        /// Complete optimization request (called by PromptOptimizationResponseHandler)
        /// </summary>
        public void CompleteRequest(string requestId, PromptOptimizationResponseEvent response)
        {
            if (_pendingRequests.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(response);
                _logger.LogDebug("Completed optimization request: {RequestId}", requestId);
            }
            else
            {
                _logger.LogWarning("Received PromptOptimizationResponse for unknown RequestId: {RequestId}", requestId);
            }
        }
    }
}
