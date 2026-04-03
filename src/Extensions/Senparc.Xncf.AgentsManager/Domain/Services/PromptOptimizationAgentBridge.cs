using System;
using System.Collections.Concurrent;
using System.Threading;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// Pass results between Agent conversations and HTTP optimization requests (CreateOptimizedPrompt writes, Handler reads and publishes response events).
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
        /// Each optimization RequestId is only allowed once <see cref="PromptItemService.AddPromptItemAsync"/> (the tool or Kernel fallback will be written by whoever grabs it first).
        /// Solution: The tool has been inserted into DB but <see cref="RecordCreatedPrompt"/> is not written to State → TryTakeResult fails → fall back and insert another one (with the same content).
        /// </summary>
        private readonly ConcurrentDictionary<string, byte> _versionInsertClaimed = new();

        /// <summary>
        /// Unify the key format to avoid _states matching failure (no registered session) caused by inconsistency between the case of the GUID passed in by the model/client and <see cref="Guid.ToString"/>.
        /// </summary>
        private static string NormalizeRequestKey(string requestId) =>
            string.IsNullOrWhiteSpace(requestId) ? null : requestId.Trim().ToLowerInvariant();

        /// <summary> Called before calling AddPromptItemAsync; only returns true the first time. </summary>
        public bool TryClaimVersionInsert(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            return key != null && _versionInsertClaimed.TryAdd(key, 0);
        }

        /// <summary> Released when AddPromptItemAsync fails, allowing Kernel to fall back and retry writing to the library. </summary>
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
        /// Normalize the optimizationRequestId passed in by the model to the <see cref="BeginRequest"/> registered key (it is forbidden to blindly fill in the Guid and cause an orphan state).
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
        /// Uniformly clear all Bridge states (_states + _versionInsertClaimed) of the specified requestId.
        /// should be called in the finally of ChatTaskHandler to ensure that no residue is left regardless of success/failure.
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
