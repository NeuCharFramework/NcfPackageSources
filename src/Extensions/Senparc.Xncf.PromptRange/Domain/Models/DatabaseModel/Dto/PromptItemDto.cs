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
        /// 昵称
        /// </summary>
        public string NickName { get; private set; }

        /// <summary>
        /// Prompt内容
        /// </summary>
        public string Content { get; private set; }

        public int ModelId { get; private set; }

        #region Model Config

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; private set; }

        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature { get; private set; }

        /// <summary>
        /// 最大 Token 数
        /// </summary>
        public int MaxToken { get; private set; }

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public float FrequencyPenalty { get; private set; }

        public float PresencePenalty { get; private set; }

        /// <summary>
        /// 停止序列（JSON 数组） 
        /// </summary>
        [CanBeNull]
        public string StopSequences { get; private set; }

        #endregion

        #region 打分

        /// <summary>
        /// 评估参数, 平均分
        /// </summary>
        public int EvalAvgScore { get; private set; } = -1;

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvalMaxScore { get; private set; } = -1;

        /// <summary>
        /// 期望结果Json
        /// </summary>
        public string ExpectedResultsJson { get; private set; }

        #endregion


        #region Full Version


        public string FullVersion
        {
            get { return $"{RangeName}-T{Tactic}-A{Aiming}"; }
            private set { }
        }
        
        public string RangeName { get; private set; }
        
        public string Tactic { get; private set; }

        /// <summary>
        /// <para>为打靶次数，int</para>
        /// </summary>
        public int Aiming { get; private set; }

        /// <summary>
        /// 父Tactic, 可以是空串
        /// </summary>
        public string ParentTac { get; private set; }

        #endregion

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        /// 最后一次运行时间
        /// </summary>
        public DateTime LastRunTime { get; private set; } = DateTime.Now;

        /// <summary>
        /// 是否公开
        /// </summary>
        public bool IsShare { get; private set; } = false;

        public bool IsDraft { get; private set; }

        #region Prompt请求参数

        public string Prefix { get; private set; }
        public string Suffix { get; private set; }

        public string VariableDictJson { get; private set; }

        #endregion

        public PromptItemDto()
        {
        }
    }
}