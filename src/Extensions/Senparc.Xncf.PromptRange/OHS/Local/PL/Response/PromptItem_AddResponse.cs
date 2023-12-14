using System;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptItem_AddResponse : BaseResponse
    {
        public string PromptContent { get; set; }


        /// <summary>
        /// 版本号，格式为 yyyy.MM.dd.Version
        /// </summary>
        public string Version { get; set; }

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

        /// <summary>
        /// 停止序列（JSON 数组）
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        /// <summary>
        /// Note
        /// </summary>
        public string Note { get; set; }

        public List<PromptResult> PromptResultList { get; set; }

        public PromptItem_AddResponse(string promptContent, string version, int modelId,
            int maxToken, float temperature, float topP, float frequencyPenalty, string stopSequences)
        {
            PromptContent = promptContent;
            LastRunTime = DateTime.Now;
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