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
        public int Id { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// Prompt内容
        /// </summary>
        public string Content { get; set; }

        #region Model Config

        public int ModelId { get; set; }

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// 最大 Token 数
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public float FrequencyPenalty { get; set; }

        public float PresencePenalty { get; set; }

        /// <summary>
        /// 停止序列（JSON 数组） 
        /// </summary>
        [CanBeNull]
        public string StopSequences { get; set; }

        #endregion

        #region 打分

        /// <summary>
        /// 评估参数, 平均分
        /// </summary>
        public int EvalAvgScore { get; set; } = -1;

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvalMaxScore { get; set; } = -1;

        /// <summary>
        /// 期望结果Json
        /// </summary>
        public string ExpectedResultsJson { get; set; }

        #endregion


        #region Full Version

        public string FullVersion
        {
            get { return $"{RangeName}-T{Tactic}-A{Aiming}"; }
            set { }
        }

        public string RangeName { get; set; }

        public string Tactic { get; set; }

        /// <summary>
        /// <para>为打靶次数，int</para>
        /// </summary>
        public int Aiming { get; set; }

        /// <summary>
        /// 父Tactic, 可以是空串
        /// </summary>
        public string ParentTac { get; set; }

        #endregion

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// 最后一次运行时间
        /// </summary>
        public DateTime LastRunTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否公开
        /// </summary>
        public bool IsShare { get; set; } = false;

        public bool IsDraft { get; set; }

        #region Prompt请求参数

        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public string VariableDictJson { get; set; }

        #endregion

        private PromptItemDto()
        {
        }
    }
}