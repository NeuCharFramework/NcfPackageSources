using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// PromptItem
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptItem))]/*必须添加前缀，防止全系统中发生冲突*/
    [Serializable]
    public class PromptItem : EntityBase<int>
    {
        public int PromptGroupId { get; private set; }

        /// <summary>
        /// 最大 Token 数
        /// </summary>
        public int MaxToken { get; private set; }

        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature { get; private set; }

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; private set; }

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public float FrequencyPenalty { get; private set; }

        /// <summary>
        /// 每个 Prompt 的结果数
        /// </summary>
        public int ResultsPerPrompt { get; private set; }

        /// <summary>
        /// 停止序列
        /// </summary>
        public string[] StopSequences { get; private set; }

        /// <summary>
        /// 聊天系统 Prompt
        /// </summary>
        public string ChatSystemPrompt { get; private set; }

        /// <summary>
        /// Token 选择偏好
        /// </summary>
        public float[] TokenSelectionBiases { get; private set; }

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvaluationScore { get; private set; }

        public PromptGroup PromptGroup { get; private set; }

        private PromptItem() { }

        public PromptItem(int promptGroupId, int maxToken, float temperature, float topP, float frequencyPenalty, int resultsPerPrompt, string[] stopSequences, string chatSystemPrompt, float[] tokenSelectionBiases, int evaluationScore)
        {
            PromptGroupId = promptGroupId;
            MaxToken = maxToken;
            Temperature = temperature;
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            ResultsPerPrompt = resultsPerPrompt;
            StopSequences = stopSequences;
            ChatSystemPrompt = chatSystemPrompt;
            TokenSelectionBiases = tokenSelectionBiases;
            EvaluationScore = evaluationScore;
        }

        public PromptItem(PromptItemDto promptItemDto)
        {
            MaxToken = promptItemDto.MaxToken;
            Temperature = promptItemDto.Temperature;
            TopP = promptItemDto.TopP;
            FrequencyPenalty = promptItemDto.FrequencyPenalty;
            ResultsPerPrompt = promptItemDto.ResultsPerPrompt;
            StopSequences = promptItemDto.StopSequences;
            ChatSystemPrompt = promptItemDto.ChatSystemPrompt;
            TokenSelectionBiases = promptItemDto.TokenSelectionBiases;
            EvaluationScore = promptItemDto.EvaluationScore;
        }
    }
}
