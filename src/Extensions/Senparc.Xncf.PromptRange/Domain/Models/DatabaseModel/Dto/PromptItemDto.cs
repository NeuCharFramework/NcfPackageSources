using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class PromptItemDto : DtoBase
    {
        /// <summary>
        /// Prompt内容
        /// </summary>
        public string PromptContent { get; private set; }
        /// <summary>
        /// PromptGroupId
        /// </summary>
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
        /// 停止序列（JSON 数组）
        /// </summary>
        public string StopSequences { get; private set; }

        /// <summary>
        /// 从 StopSequences 自动获取数组，如果为空，则返回空对象
        /// </summary>
        public string[] StopSequencesArray => (StopSequences ?? "[]").GetObject<string[]>();

        /// <summary>
        /// 聊天系统 Prompt
        /// </summary>
        public string ChatSystemPrompt { get; private set; }

        /// <summary>
        /// Token 选择偏好
        /// </summary>
        public string TokenSelectionBiases { get; private set; }

        /// <summary>
        /// 从 TokenSelectionBiases 自动获取数组，如果为空，则返回空对象
        /// </summary>
        public float[] TokenSelectionBiasesArray => (TokenSelectionBiases ?? "[]").GetObject<float[]>();

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvaluationScore { get; private set; }
        /// <summary>
        /// EvaluationMetrics
        /// </summary>
        public string EvaluationMetrics { get; private set; }


        /// <summary>
        /// 版本号，格式为 yyyy.MM.dd.Version
        /// </summary>
        public virtual string Version { get; private set; }

        /// <summary>
        /// 最后一次运行时间
        /// </summary>
        public DateTime LastRunTime { get; private set; }

        //private PromptItemDto() { }

        public PromptItemDto() { }

    }
}