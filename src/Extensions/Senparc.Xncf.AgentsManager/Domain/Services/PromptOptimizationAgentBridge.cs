using System.Collections.Concurrent;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// 在 Agent 对话与 HTTP 优化请求之间传递结果（CreateOptimizedPrompt 写入，ChatTask Handler 读取后发布响应事件）
    /// </summary>
    public class PromptOptimizationAgentBridge
    {
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

        public void BeginRequest(string requestId)
        {
            _states[requestId] = new State();
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
