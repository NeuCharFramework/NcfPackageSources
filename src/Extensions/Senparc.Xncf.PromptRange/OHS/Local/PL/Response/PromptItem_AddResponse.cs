using System;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptItem_AddResponse : BaseResponse
    {
        public string PromptContent { get; set; }


        /// <summary>
        /// 完整版本号
        /// </summary>
        public string FullVersion { get; set; }

        #region model config

        public int ModelId { get; set; }

        /// <summary>
        /// 最大 Token 数
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; set; }

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public float FrequencyPenalty { get; set; }

        public float PresencePenalty { get; private set; }

        /// <summary>
        /// 停止序列（JSON 数组）
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        /// <summary>
        /// Note
        /// </summary>
        public string Note { get; set; }

        public new DateTime LastRunTime { get; set; } = DateTime.Now;

        public bool IsDraft { get; set; }
        public bool IsShare { get; set; }

        /// <summary>
        /// 期望结果 - Json 类型为List &lt; string &gt;
        /// </summary>
        public string ExpectedResultsJson { get; set; }

        public List<PromptResult> PromptResultList { get; set; } = new();

        /// <summary>
        /// 评估参数, 平均分
        /// </summary>
        public int EvalAvgScore { get; set; }

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvalMaxScore { get; set; }

        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string VariableDictJson { get; set; }

        public PromptItem_AddResponse(int promptItemId, string promptContent, string fullVersion, int modelId,
            int maxToken, float temperature, float topP, float frequencyPenalty, float presencePenalty, string stopSequences, string note,
            string expectedResultsJson, string prefix, string suffix, string variableDictJson, int evalAvgScore, int evalMaxScore,
            bool isDraft, bool isShare)
        {
            Id = promptItemId;
            PromptContent = promptContent;
            FullVersion = fullVersion;
            ModelId = modelId;
            MaxToken = maxToken;
            Temperature = temperature;
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
            StopSequences = stopSequences;
            Note = note;
            ExpectedResultsJson = expectedResultsJson;
            Prefix = prefix;
            Suffix = suffix;
            VariableDictJson = variableDictJson;
            EvalAvgScore = evalAvgScore;
            EvalMaxScore = evalMaxScore;
            IsDraft = isDraft;
            IsShare = isShare;
        }

        public PromptItem_AddResponse(PromptItem item) : this(item.Id, item.Content, item.FullVersion, item.ModelId,
            item.MaxToken, item.Temperature, item.TopP, item.FrequencyPenalty, item.PresencePenalty, item.StopSequences, item.Note,
            item.ExpectedResultsJson, item.Prefix, item.Suffix, item.VariableDictJson, item.EvalAvgScore, item.EvalMaxScore,
            item.IsDraft, item.IsShare)
        {
        }

        public PromptItem_AddResponse(PromptItemDto item) : this(item.Id, item.Content, item.FullVersion, item.ModelId,
            item.MaxToken, item.Temperature, item.TopP, item.FrequencyPenalty, item.PresencePenalty, item.StopSequences, item.Note,
            item.ExpectedResultsJson, item.Prefix, item.Suffix, item.VariableDictJson, item.EvalAvgScore, item.EvalMaxScore,
            item.IsDraft, item.IsShare)
        {
        }
    }
}