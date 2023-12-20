using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// PromptItem
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptItem))] /*必须添加前缀，防止全系统中发生冲突*/
    [Serializable]
    public class PromptItem : EntityBase<int>
    {
        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName { get; private set; }

        /// <summary>
        /// Prompt内容
        /// </summary>
        public string Content { get; private set; }

        [Required] public int ModelId { get; private set; }

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
        /// 停止序列（JSON 数组） //todo 真的可以为null吗？
        /// </summary>
        [CanBeNull]
        public string StopSequences { get; private set; }

        #endregion

        /// <summary>
        /// 每个 Prompt 的结果数
        /// </summary>
        [Obsolete("已废弃")]
        public int NumsOfResults { get; private set; }


        /// <summary>
        /// 聊天系统 Prompt
        /// </summary>
        [Obsolete("已废弃")]
        public string ChatSystemPrompt { get; private set; } = "";

        /// <summary>
        /// Token 选择偏好
        /// </summary>
        [Obsolete("已废弃")]
        public string TokenSelectionBiases { get; private set; }

        #region 打分

        /// <summary>
        /// 评估参数, 平均分
        /// </summary>
        [MaxLength(3)]
        public int EvalAvgScore { get; private set; } = -1;

        /// <summary>
        /// 评估参数
        /// </summary>
        [MaxLength(3)]
        public int EvalMaxScore { get; private set; } = -1;

        /// <summary>
        /// 期望结果Json
        /// </summary>
        public string ExpectedResultsJson { get; private set; }

        #endregion


        #region Full Version

        /// <summary>
        /// <para>版本号，格式为 Name-Tactic-Aiming</para> 
        /// <example>2023.12.14.1-T1.1-A123</example>
        /// <para>Name: <inheritdoc cref="RangeName"/></para>
        /// <para>Tactic: <inheritdoc cref="Tactic"/></para>
        /// <para>Aiming: <inheritdoc cref="Aiming"/></para>
        ///         为   Tx              这里的x为分支号，str,允许1.1.1。。。
        ///      Aiming   为   Ax              这里的x为打靶次数，int
        /// </summary>
        [Required]
        public string FullVersion
        {
            get { return $"{RangeName}-T{Tactic}-A{Aiming}"; }
            private set { }
        }

        /// <summary>
        /// 靶场名
        /// <para>格式为 yyyy.MM.dd.x ,这里的x为当天生成的序号，int</para>
        /// <example>示例一
        ///     <code>2023.12.14.1</code>
        /// </example>
        /// <example>示例二
        ///     <code>2023.12.14.123</code>
        /// </example>
        /// </summary>
        [MaxLength(20)]
        public string RangeName { get; private set; }

        /// <summary>
        /// 靶道名
        /// <para>格式为 x.x.x... ,这里的x为分支号</para>
        /// <para>在完整的版本号中，应该用-T连接Name</para>
        /// 
        /// </summary>
        public string Tactic { get; private set; }

        /// <summary>
        /// <para>为打靶次数，int</para>
        /// </summary>
        [MaxLength(5)]
        public int Aiming { get; private set; }

        /// <summary>
        /// 父Tactic, 可以是空串
        /// </summary>
        [Required]
        public string ParentTac { get; private set; }

        #endregion

        /// <summary>
        /// Note（可选）
        /// </summary>
        [MaxLength(20)]
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


       

        #region ctor 构造函数

        public PromptItem(string rangeName, string content, int modelId, float topP, float temperature, int maxToken, float frequencyPenalty,
            float presencePenalty, string stopSequences, int numsOfResults, int evalAvgScore,
            string fullVersion, DateTime lastRunTime, string note, bool isDraft)
        {
            RangeName = rangeName;
            Content = content;
            ModelId = modelId;
            TopP = topP;
            Temperature = temperature;
            MaxToken = maxToken;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
            StopSequences = stopSequences;
            NumsOfResults = numsOfResults;
            EvalAvgScore = evalAvgScore;
            FullVersion = fullVersion;
            LastRunTime = lastRunTime;
            Note = note;
            IsDraft = isDraft;
        }

        public PromptItem(string content, int modelId, float topP, float temperature, int maxToken, float frequencyPenalty, float presencePenalty,
            string stopSequences, int numsOfResults, string rangeName, string tactic, int aiming, string parentTac, string note,
            string expectedResultsJson, bool isDraft)
        {
            Content = content;
            ModelId = modelId;
            TopP = topP;
            Temperature = temperature;
            MaxToken = maxToken;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
            StopSequences = stopSequences;
            NumsOfResults = numsOfResults;
            RangeName = rangeName;
            Tactic = tactic;
            Aiming = aiming;
            ParentTac = parentTac;
            Note = note;
            ExpectedResultsJson = expectedResultsJson;
            IsDraft = isDraft;
        }

        #endregion

        public PromptItem Switch(bool show)
        {
            this.IsShare = show;

            return this;
        }

        public PromptItem ModifyNickName(string nickName)
        {
            this.NickName = nickName;

            return this;
        }

        public PromptItem UpdateExpectedResultsJson(string expectedResultsJson)
        {
            this.ExpectedResultsJson = expectedResultsJson;

            return this;
        }

        public PromptItem UpdateEvalAvgScore(int score)
        {
            this.EvalAvgScore = score;

            return this;
        }

        public PromptItem UpdateEvalMaxScore(int score)
        {
            this.EvalMaxScore = score;

            return this;
        }
    }
}