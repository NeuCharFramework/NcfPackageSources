using System;
using System.Collections.Concurrent;
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

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class PromptOptimizationService
    {
        private readonly IEventBus _eventBus;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly ChatGroupService _chatGroupService;
        private readonly ChatGroupMemberService _chatGroupMemberService;
        private readonly ILogger<PromptOptimizationService> _logger;
        
        // 用于存储挂起的请求：RequestId -> TCS
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<PromptOptimizationResponseEvent>> _pendingRequests 
            = new ConcurrentDictionary<string, TaskCompletionSource<PromptOptimizationResponseEvent>>();
            
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<PromptInitResponseEvent>> _pendingInitRequests 
            = new ConcurrentDictionary<string, TaskCompletionSource<PromptInitResponseEvent>>();

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
        /// 确保 PromptCatalyzer Agent 和相关资源已初始化
        /// </summary>
        /// <param name="modelId">可选：用户指定的 AI Model ID</param>
        public async Task<PromptInitResponseEvent> EnsureInitializedAsync(int? modelId = null)
        {
            _logger.LogInformation("========== EnsureInitializedAsync 开始 ==========");
            _logger.LogInformation("请求的 ModelId: {ModelId}", modelId);

            // === 步骤1：检查 Agent 是否已存在 ===
            _logger.LogInformation("【步骤1/3】检查 PromptCatalyzer Agent 是否已存在...");
            var agent = _agentsTemplateService.GetObject(z => z.Name == "PromptCatalyzer");
            
            if (agent != null)
            {
                _logger.LogInformation("  ✅ Agent 已存在，ID: {AgentId}, PromptCode: {PromptCode}", 
                    agent.Id, agent.SystemMessage);
                return new PromptInitResponseEvent(
                    Guid.Empty.ToString(), 
                    agent.SystemMessage,  // SystemMessage 存储 PromptCode
                    true, 
                    "Already initialized");
            }
            
            // === 步骤2：Agent 不存在，通过 EventBus 请求创建 PromptItem ===
            _logger.LogInformation("【步骤2/3】Agent 不存在，开始初始化流程...");
            
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
                
                // 发布初始化请求事件（包含 ModelId）
                var requestEvent = new PromptInitRequestEvent(requestId, modelId);
                await _eventBus.PublishAsync(requestEvent);
                
                _logger.LogInformation("  等待 PromptInitResponseEvent（最长2分钟）...");
                
                // 等待 PromptRange 创建 Prompt 并返回 PromptCode（设置 2 分钟超时）
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _pendingInitRequests.TryRemove(requestId, out _);
                    _logger.LogError("  ❌ 等待 PromptInitResponseEvent 超时（2分钟）");
                    throw new TimeoutException("PromptCatalyzer 初始化超时（2 分钟）");
                }
                
                var response = await tcs.Task;
                _logger.LogInformation("  ✅ 收到 PromptInitResponseEvent: Success={Success}, PromptCode={PromptCode}, Message={Message}", 
                    response.Success, response.PromptCode, response.ErrorMessage);
                
                if (!response.Success)
                {
                    throw new Exception($"Prompt 初始化失败: {response.ErrorMessage}");
                }
                
                if (string.IsNullOrEmpty(response.PromptCode))
                {
                    throw new Exception("Prompt 初始化返回的 PromptCode 为空");
                }
                
                // === 步骤3：创建 Agent ===
                _logger.LogInformation("【步骤3/3】创建 PromptCatalyzer Agent...");
                _logger.LogInformation("  PromptCode: {PromptCode}", response.PromptCode);
                
                var newAgent = new AgentTemplate(
                    name: "PromptCatalyzer",
                    systemMessage: response.PromptCode,  // 存储 PromptCode 用于后续调用
                    enable: true,
                    description: "自动优化 Prompt 内容和参数（Temperature 等）的 AI Agent",
                    promptCode: response.PromptCode,
                    hookRobotType: HookRobotType.None,
                    hookRobotParameter: null,
                    avastar: null,
                    functionCallNames: null,
                    mcpEndpoints: null
                );
                
                await _agentsTemplateService.SaveObjectAsync(newAgent);
                _logger.LogInformation("  ✅ Agent 创建成功！AgentId: {AgentId}, PromptCode: {PromptCode}", 
                    newAgent.Id, response.PromptCode);
                
                _logger.LogInformation("========== EnsureInitializedAsync 完成 ==========");
                
                // TODO: 6-7. 创建 ChatGroup 和绑定 Agent（暂时注释，需要完善 AgentTemplate ID 设置）
                // 因为 ChatGroup 需要 adminAgentTemplateId 和 enterAgentTemplateId，这里需要更完善的逻辑
                
                return response;
            }
            catch (Exception ex)
            {
                _pendingInitRequests.TryRemove(requestId, out _);
                _logger.LogError(ex, "❌ EnsureInitializedAsync 失败");
                throw;
            }
        }

        /// <summary>
        /// 完成初始化请求（由 PromptInitResponseHandler 调用）
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
        /// 优化指定的 Prompt（包括内容和参数）
        /// </summary>
        public async Task<PromptOptimizationResponseEvent> OptimizePromptAsync(
            string promptCode, 
            string promptContent,
            string userRequirement,
            OptimizationContext context)
        {
            // 1. 确保 Agent 已初始化
            await EnsureInitializedAsync();

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

                // 2. 发布优化请求事件
                var requestEvent = new PromptOptimizationRequestEvent(
                    requestId, 
                    promptCode, 
                    promptContent,
                    userRequirement,
                    context);
                await _eventBus.PublishAsync(requestEvent);

                // 3. 等待响应（设置 5 分钟超时，因为 AI 处理可能较慢）
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _pendingRequests.TryRemove(requestId, out _);
                    throw new TimeoutException("Prompt 优化请求超时（5 分钟）");
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

        /// <summary>
        /// 完成优化请求（由 PromptOptimizationResponseHandler 调用）
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
