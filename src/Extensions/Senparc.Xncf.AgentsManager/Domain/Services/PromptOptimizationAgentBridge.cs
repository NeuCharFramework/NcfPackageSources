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
    /// Maintains async-local and fallback correlation state between Agent optimization
    /// requests and PromptRange optimization results.
    /// </summary>
    public class PromptOptimizationAgentBridge
    {
        private static readonly AsyncLocal<string> ActiveOptimizationRequestId = new();

        /// <summary>Begins an async-local optimization request scope.</summary>
        /// <param name="requestId">The correlation identifier.</param>
        /// <returns>A disposable scope restoring the previous request ID.</returns>
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

        /// <summary>Gets the current async-local or fallback correlation ID.</summary>
        /// <returns>The normalized request ID, or <see langword="null"/> when none exists.</returns>
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

        /// <summary>Sets the process fallback correlation ID.</summary>
        /// <param name="requestId">The correlation identifier.</param>
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

        /// <summary>Clears the process fallback correlation ID.</summary>
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

            /// <summary>Creates a restoration scope for the previous request ID.</summary>
            /// <param name="previous">The previous request ID.</param>
            public ActiveRequestScope(string previous) => _previous = previous;

            /// <summary>Restores the previous request ID once.</summary>
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
            /// <summary>Completes a no-op scope.</summary>
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
        /// Stores pending optimization output and evaluation data by normalized request ID.
        /// </summary>
        private static readonly ConcurrentDictionary<string, State> _states = new();

        /// <summary>
        /// Tracks request IDs whose prompt version has already been inserted.
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> _versionInsertClaimed = new();

        /// <summary>
        /// Normalizes a request correlation ID for dictionary and async-local lookup.
        /// </summary>
        private static string NormalizeRequestKey(string requestId) =>
            string.IsNullOrWhiteSpace(requestId) ? null : requestId.Trim().ToLowerInvariant();

        /// <summary>
        /// Claims the one-time version-insert slot for a request.
        /// </summary>
        /// <param name="requestId">The request correlation ID.</param>
        /// <returns><see langword="true"/> when the claim was obtained.</returns>
        public bool TryClaimVersionInsert(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            return key != null && _versionInsertClaimed.TryAdd(key, 0);
        }

        /// <summary>Releases a previously claimed version-insert slot.</summary>
        /// <param name="requestId">The request correlation ID.</param>
        public void ReleaseVersionInsertClaim(string requestId)
        {
            var key = NormalizeRequestKey(requestId);
            if (key != null)
            {
                _versionInsertClaimed.TryRemove(key, out _);
            }
        }

        /// <summary>Initializes request state and clears a stale version claim.</summary>
        /// <param name="requestId">The request correlation ID.</param>
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
        /// Resolves a model-supplied or active correlation candidate to a registered request key.
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
                _states.TryAdd(fb, new State());
                registeredKey = fb;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the PromptRange version recorded for a request.
        /// </summary>
        /// <param name="requestId">The request correlation ID.</param>
        /// <param name="fullVersion">Receives the recorded version.</param>
        /// <returns><see langword="true"/> when a version was recorded.</returns>
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

        /// <summary>
        /// Records an optimization-created PromptRange version and generation parameters.
        /// </summary>
        /// <param name="requestId">The request correlation ID.</param>
        /// <param name="newPromptCode">The created PromptRange version.</param>
        /// <param name="newPromptContent">The generated prompt content.</param>
        /// <param name="temperature">The generation temperature.</param>
        /// <param name="topP">The generation top-p value.</param>
        /// <param name="maxTokens">The generation token limit.</param>
        /// <param name="frequencyPenalty">The frequency penalty.</param>
        /// <param name="presencePenalty">The presence penalty.</param>
        /// <param name="evaluationReason">Optional evaluation explanation.</param>
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

        /// <summary>
        /// Atomically consumes the recorded optimization result for a request.
        /// </summary>
        /// <param name="requestId">The request correlation ID.</param>
        /// <param name="response">Receives the optimization response event.</param>
        /// <returns><see langword="true"/> when a result was available and consumed.</returns>
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

        /// <summary>Removes recorded optimization state without producing a response.</summary>
        /// <param name="requestId">The request correlation ID.</param>
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
        /// Removes all recorded state and version claims for a request.
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
