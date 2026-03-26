using System;
using System.Collections.Concurrent;
using System.Threading;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// 在 Agent 对话与 HTTP 优化请求之间传递结果（CreateOptimizedPrompt 写入，ChatTask Handler 读取后发布响应事件）
    /// </summary>
    public class PromptOptimizationAgentBridge
    {
        /// <summary>
        /// 当前正在执行的优化请求 ID（在 RunChatGroupExecutionCoreAsync 内设置，供 Plugin 在模型漏传参数时仍能关联）
        /// </summary>
        private static readonly AsyncLocal<string> ActiveOptimizationRequestId = new();

        /// <summary>
        /// 在 ChatGroup 执行期间挂起 RequestId；Dispose 时恢复外层（支持嵌套时恢复上一值）
        /// </summary>
        public static IDisposable BeginActiveRequestScope(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return NullScope.Instance;
            }

            var previous = ActiveOptimizationRequestId.Value;
            ActiveOptimizationRequestId.Value = requestId.Trim();
            return new ActiveRequestScope(previous);
        }

        /// <summary>
        /// 优先 AsyncLocal（同异步流）；否则使用 RunChatGroup 设置的兜底关联（工具可能在未传播 ExecutionContext 的线程上执行）
        /// </summary>
        public static string TryGetActiveRequestId()
        {
            var local = ActiveOptimizationRequestId.Value;
            if (!string.IsNullOrWhiteSpace(local))
            {
                return local;
            }

            lock (FallbackCorrelationLock)
            {
                return _fallbackCorrelationId;
            }
        }

        private static readonly object FallbackCorrelationLock = new object();
        private static string _fallbackCorrelationId;

        public static void SetFallbackCorrelationId(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return;
            }

            lock (FallbackCorrelationLock)
            {
                _fallbackCorrelationId = requestId.Trim();
            }
        }

        public static void ClearFallbackCorrelationId()
        {
            lock (FallbackCorrelationLock)
            {
                _fallbackCorrelationId = null;
            }
        }

        private sealed class ActiveRequestScope : IDisposable
        {
            private readonly string _previous;
            private bool _disposed;

            public ActiveRequestScope(string previous) => _previous = previous;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                ActiveOptimizationRequestId.Value = _previous;
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }

        private sealed class State
        {
            public string NewPromptCode;
            public string NewPromptContent;
            public float Temperature;
            public float TopP;
            public int MaxTokens;
            public float FrequencyPenalty;
            public float PresencePenalty;
            public string EvaluationReason = "";

            // 0 = not started, 1 = creation already claimed; use Interlocked to avoid TOCTOU race
            private int _creationStarted = 0;

            /// <summary>
            /// Returns true only for the first caller; subsequent callers get false (creation already in progress/done).
            /// </summary>
            public bool TryClaimCreation() =>
                System.Threading.Interlocked.CompareExchange(ref _creationStarted, 1, 0) == 0;
        }

        private readonly ConcurrentDictionary<string, State> _states = new();

        public void BeginRequest(string requestId)
        {
            _states[requestId] = new State();
        }

        /// <summary>
        /// 原子地标记"创建已开始"。只有第一个调用者获得 true（允许继续创建 DB 记录）；
        /// 后续调用（重复触发）得到 false，插件应跳过 DB 写入。
        /// </summary>
        public bool TryClaimCreation(string requestId)
        {
            return _states.TryGetValue(requestId, out var state) && state.TryClaimCreation();
        }

        /// <summary>
        /// 由 PromptOptimizationPlugin.CreateOptimizedPrompt 在成功创建新版本后调用
        /// </summary>
        public void RecordCreatedPrompt(
            string requestId,
            string newPromptCode,
            string newPromptContent,
            float temperature,
            float topP,
            int maxTokens,
            float frequencyPenalty,
            float presencePenalty,
            string evaluationReason = null)
        {
            if (!_states.TryGetValue(requestId, out var state))
            {
                state = new State();
                _states[requestId] = state;
            }

            state.NewPromptCode = newPromptCode;
            state.NewPromptContent = newPromptContent ?? "";
            state.Temperature = temperature;
            state.TopP = topP;
            state.MaxTokens = maxTokens;
            state.FrequencyPenalty = frequencyPenalty;
            state.PresencePenalty = presencePenalty;
            state.EvaluationReason = evaluationReason ?? "Agent 已通过工具创建优化版本";
        }

        public bool TryTakeResult(string requestId, out PromptOptimizationResponseEvent response)
        {
            response = null;
            if (!_states.TryRemove(requestId, out var state) || string.IsNullOrWhiteSpace(state.NewPromptCode))
            {
                return false;
            }

            response = new PromptOptimizationResponseEvent(
                requestId,
                state.NewPromptCode,
                state.NewPromptContent,
                new OptimizedParameters(
                    state.Temperature,
                    state.TopP,
                    state.MaxTokens,
                    state.FrequencyPenalty,
                    state.PresencePenalty),
                0,
                state.EvaluationReason,
                true,
                null);
            return true;
        }

        public void Remove(string requestId) => _states.TryRemove(requestId, out _);
    }
}
