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

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class PromptOptimizationService
    {
        private readonly IEventBus _eventBus;
        private readonly AgentsTemplateService _agentsTemplateService;
        
        // 用于存储挂起的请求：RequestId -> TCS
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<PromptOptimizationResponseEvent>> _pendingRequests 
            = new ConcurrentDictionary<string, TaskCompletionSource<PromptOptimizationResponseEvent>>();
            
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<PromptInitResponseEvent>> _pendingInitRequests 
            = new ConcurrentDictionary<string, TaskCompletionSource<PromptInitResponseEvent>>();

        public PromptOptimizationService(IEventBus eventBus, AgentsTemplateService agentsTemplateService)
        {
            _eventBus = eventBus;
            _agentsTemplateService = agentsTemplateService;
        }

        public async Task<PromptInitResponseEvent> EnsureInitializedAsync()
        {
            var agent = _agentsTemplateService.GetObject(z => z.Name == "PromptCatalyzer");
            if (agent != null)
            {
                // Agent 已存在，假定 PromptRange 也就绪 (或者可以进一步检查 PromptCode)
                return new PromptInitResponseEvent(Guid.Empty.ToString(), agent.SystemMessage /* Assuming PromptCode is stored here */, true, "Already initialized");
            }
            
            // Agent 不存在，开始初始化流程
            var requestId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<PromptInitResponseEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pendingInitRequests.TryAdd(requestId, tcs))
            {
                 throw new NcfExceptionBase("Failed to register init request ID.");
            }
            
            var requestEvent = new PromptInitRequestEvent(requestId);
            await _eventBus.PublishAsync(requestEvent);
            
            var response = await tcs.Task; // 等待 PromptRange 创建完成并返回 PromptCode
            
            // 创建 Agent
            if (response.Success)
            {
                // TODO: Store PromptCode: response.PromptCode into new Agent "PromptCatalyzer"
                // var newAgent = new AgentTemplate { ... SystemMessage = response.PromptCode ... };
                // await _agentsTemplateService.SaveObjectAsync(newAgent);
                
                // TODO: Create ChatGroup "PromptCatalyzerEvents" and bind Agent
            }
            
            return response;
        }

        public void CompleteInitRequest(string requestId, PromptInitResponseEvent response)
        {
            if (_pendingInitRequests.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(response);
            }
        }

        public async Task<PromptOptimizationResponseEvent> OptimizePromptAsync(string promptCode, string userRequirement)
        {
            var requestId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<PromptOptimizationResponseEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_pendingRequests.TryAdd(requestId, tcs))
            {
                throw new InvalidOperationException("Failed to register request ID.");
            }

            var requestEvent = new PromptOptimizationRequestEvent(requestId, promptCode, userRequirement);
            await _eventBus.PublishAsync(requestEvent);

            // 设置超时机制 (例如 5 分钟)
            // 实际生产中建议使用 CancellationTokenSource 和 Task.WhenAny
            return await tcs.Task; 
        }

        public void CompleteRequest(string requestId, PromptOptimizationResponseEvent response)
        {
            if (_pendingRequests.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(response);
            }
        }
    }
}
