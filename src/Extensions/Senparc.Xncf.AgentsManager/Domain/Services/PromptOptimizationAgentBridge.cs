/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptOptimizationAgentBridge.cs
    File description: Related implementation.

    Created by: Senparc - 20260325

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Threading;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static readonly ConcurrentDictionary<string, State> _states = new();

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> _versionInsertClaimed = new();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static string NormalizeRequestKey(string requestId) =>
            string.IsNullOrWhiteSpace(requestId) ? null : requestId.Trim().ToLowerInvariant();

        ///Legacy comment translated to English.
        public bool TryClaimVersionInsert(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            return key != null && _versionInsertClaimed.TryAdd(key, 0);
        }

        ///Legacy comment translated to English.
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

            _states[key] = new State();
            _versionInsertClaimed.TryRemove(key, out _);
        }

        /// <summary>
        ///Legacy comment translated to English.
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
            if (active != null)
            {
                //Legacy comment translated to English.
                _states.TryAdd(active, new State());
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
            if (fb != null)
            {
                //Legacy comment translated to English.
                _states.TryAdd(fb, new State());
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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
