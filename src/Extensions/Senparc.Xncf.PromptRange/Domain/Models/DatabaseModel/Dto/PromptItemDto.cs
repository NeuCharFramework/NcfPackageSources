using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class PromptItemDto : DtoBase
    {
        /// <summary>
        /// Prompt内容
        /// </summary>
        public string PromptContent { get; set; }

        #region llm config

        public int ModelId { get;  set; }

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
        /// 每个 Prompt 的结果数
        /// </summary>
        public int NumsOfResults { get; set; }

        /// <summary>
        /// 从 StopSequences 自动获取数组，如果为空，则返回空对象
        /// </summary>
        public string[] StopSequencesArray => (StopSequences ?? "[]").GetObject<string[]>();


        /// <summary>
        /// 版本号，格式为 yyyy.MM.dd.Version
        /// </summary>
        public virtual string FullVersion { get; set; }

        /// <summary>
        /// 最后一次运行时间
        /// </summary>
        public DateTime LastRunTime { get; set; }


        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; private set; }

        public bool IsShare { get; private set; } = false;

        public string ExpectedResultsJson { get; private set; }


        public PromptItemDto()
        {
        }
    }
}