using System;
using System.Collections.Concurrent;
using System.Threading;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// 在 Agent 对话与 HTTP 优化请求之间传递结果（CreateOptimizedPrompt 写入，Handler 读取后发布响应事件）。
    /// </summary>
    public class PromptOptimizationAgentBridge
    {
        private static readonly AsyncLocal<string> ActiveOptimizationRequestId = new();

        public static IDisposable BeginActiveRequestScope(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return NullScope.Instance;
            }

            var previous = ActiveOptimizationRequestId.Value;
            ActiveOptimizationRequestId.Value = requestId.Trim().ToLowerInvariant();
            return new ActiveRequestScope(previous);
        }

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
                _fallbackCorrelationId = requestId.Trim().ToLowerInvariant();
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
        }

        private readonly ConcurrentDictionary<string, State> _states = new();

        /// <summary>
        /// 每个优化 RequestId 只允许一次 <see cref="PromptItemService.AddPromptItemAsync"/>（工具或 Kernel 回退谁先抢到谁写）。
        /// 解决：工具已插入 DB 但 <see cref="RecordCreatedPrompt"/> 未写入 State → TryTakeResult 失败 → 回退再插一条（内容相同）。
        /// </summary>
        private readonly ConcurrentDictionary<string, byte> _versionInsertClaimed = new();

        /// <summary>
        /// 统一键格式，避免模型/客户端传入的 GUID 大小写与 <see cref="Guid.ToString"/> 不一致导致 _states 匹配失败（no registered session）。
        /// </summary>
        private static string NormalizeRequestKey(string requestId) =>
            string.IsNullOrWhiteSpace(requestId) ? null : requestId.Trim().ToLowerInvariant();

        /// <summary>在调用 AddPromptItemAsync 之前调用；仅第一次返回 true。</summary>
        public bool TryClaimVersionInsert(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            return key != null && _versionInsertClaimed.TryAdd(key, 0);
        }

        /// <summary>AddPromptItemAsync 失败时释放，允许 Kernel 回退重试写库。</summary>
        public void ReleaseVersionInsertClaim(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            if (key != null)
            {
                _versionInsertClaimed.TryRemove(key, out _);
            }
        }

        public void BeginRequest(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            if (key == null)
            {
                return;
            }

            _states.TryAdd(key, new State());
        }

        /// <summary>
        /// 将模型传入的 optimizationRequestId 归一到 <see cref="BeginRequest"/> 已注册的 key（禁止瞎填 Guid 导致孤儿 State）。
        /// </summary>
        public bool TryResolveRegisteredOptimizationKey(string candidateFromModel, out string registeredKey)
        {
            registeredKey = null;
            var c = NormalizeRequestKey(candidateFromModel);
            if (c != null && _states.ContainsKey(c))
            {
                registeredKey = c;
                return true;
            }

            var active = NormalizeRequestKey(TryGetActiveRequestId());
            if (active != null && _states.ContainsKey(active))
            {
                registeredKey = active;
                return true;
            }

            string fb;
            lock (FallbackCorrelationLock)
            {
                fb = NormalizeRequestKey(_fallbackCorrelationId);
            }

            if (fb != null && _states.ContainsKey(fb))
            {
                registeredKey = fb;
                return true;
            }

            return false;
        }

        public bool TryGetRecordedPromptCode(string requestId, out string fullVersion)
        {
            fullVersion = null;
            var key = NormalizeRequestKey(requestId);
            if (key == null || !_states.TryGetValue(key, out var state))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(state.NewPromptCode))
            {
                return false;
            }

            fullVersion = state.NewPromptCode;
            return true;
        }

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
            var key = NormalizeRequestKey(requestId);
            if (key == null)
            {
                return;
            }

            if (!_states.TryGetValue(key, out var state))
            {
                return;
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
            var key = NormalizeRequestKey(requestId);
            if (key == null)
            {
                return false;
            }

            if (!_states.TryGetValue(key, out var state))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(state.NewPromptCode))
            {
                return false;
            }

            _states.TryRemove(key, out _);
            _versionInsertClaimed.TryRemove(key, out _);

            response = new PromptOptimizationResponseEvent(
                key,
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

        public void Remove(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            if (key == null)
            {
                return;
            }

            _states.TryRemove(key, out _);
        }

        /// <summary>
        /// 统一清理指定 requestId 的所有 Bridge 状态（_states + _versionInsertClaimed）。
        /// 应在 ChatTaskHandler 的 finally 中调用，确保无论成功/失败都不留残余。
        /// </summary>
        public void CleanupRequest(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            if (key == null)
            {
                return;
            }

            _states.TryRemove(key, out _);
            _versionInsertClaimed.TryRemove(key, out _);
        }
    }
}
