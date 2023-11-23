using System;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptItem_AddResponse
    {
        public PromptItem_AddResponse()
        {
        }

        public string PromptContent { get; set; }
        public DateTime LastRunTime { get; set; }

        /// <summary>
        /// 版本号，格式为 yyyy.MM.dd.Version
        /// </summary>
        public string Version { get; set; }

        #region llm config

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

        /// <summary>
        /// 停止序列（JSON 数组）
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        public List<PromptResult> PromptResultList { get; set; } = new List<PromptResult>(0);

        public PromptItem_AddResponse(string promptContent, DateTime lastRunTime, string version, int modelId,
            int maxToken, float temperature, float topP, float frequencyPenalty, string stopSequences)
        {
            PromptContent = promptContent;
            LastRunTime = lastRunTime;
            Version = version;
            ModelId = modelId;
            MaxToken = maxToken;
            Temperature = temperature;
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            StopSequences = stopSequences;
        }
    }
}