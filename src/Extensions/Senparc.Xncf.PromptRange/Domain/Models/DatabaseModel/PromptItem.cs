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
        public int NumsOfResults { get; private set; } = 0;


        /// <summary>
        /// 聊天系统 Prompt
        /// </summary>
        public string ChatSystemPrompt { get; private set; } = "";

        /// <summary>
        /// Token 选择偏好
        /// </summary>
        public string TokenSelectionBiases { get; private set; }

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvaluationScore { get; private set; }

        #region Full Version

        /// <summary>
        /// <para>版本号，格式为 Name-Tactic-Aiming</para> 
        /// <example>2023.12.14.1-T1.1-A123</example>
        /// <para>Name: <inheritdoc cref="Name"/></para>
        /// <para>Tactic: <inheritdoc cref="Tactic"/></para>
        /// <para>Aiming: <inheritdoc cref="Aiming"/></para>
        ///         为   Tx              这里的x为分支号，str,允许1.1.1。。。
        ///      Aiming   为   Ax              这里的x为打靶次数，int
        /// </summary>
        [Required]
        public string FullVersion
        {
            get => $"{Name}-T{Tactic}-A{Aiming}";
            private set { }
        }

        /// <summary>
        /// <para>格式为 yyyy.MM.dd.x ,这里的x为当天生成的序号，int</para>
        /// <example>示例一
        ///     <code>2023.12.14.1</code>
        /// </example>
        /// <example>示例二
        ///     <code>2023.12.14.123</code>
        /// </example>
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// <para>格式为 x.x.x... ,这里的x为分支号</para>
        /// <para>在完整的版本号中，应该用-T连接Name</para>
        /// 
        /// </summary>
        public string Tactic { get; private set; }

        /// <summary>
        /// <para>为打靶次数，int</para>
        /// </summary>
        public int Aiming { get; private set; }

        // public string Version { get; private set; }
        
        [CanBeNull] public string ParentTac { get; private set; }

        #endregion

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        /// 最后一次运行时间
        /// </summary>
        public DateTime LastRunTime { get; private set; } = DateTime.Now;

        public bool IsShare { get; private set; } = false;

        private PromptItem()
        {
        }

        public PromptItem(string name, string content, int modelId, float topP, float temperature, int maxToken, float frequencyPenalty,
            float presencePenalty, string stopSequences, int numsOfResults, string chatSystemPrompt, string tokenSelectionBiases, int evaluationScore,
            string version, DateTime lastRunTime)
        {
            Name = name;
            Content = content;
            ModelId = modelId;
            TopP = topP;
            Temperature = temperature;
            MaxToken = maxToken;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
            StopSequences = stopSequences;
            NumsOfResults = numsOfResults;
            ChatSystemPrompt = chatSystemPrompt;
            TokenSelectionBiases = tokenSelectionBiases;
            EvaluationScore = evaluationScore;
            // Version = version;
            LastRunTime = lastRunTime;
        }

        public PromptItem(string name, string content, int numsOfResults)
            : this(name, content, 1, 0, 0, 0, 0, 0, "", numsOfResults, "", "", 0, "", DateTime.Now)
        {
        }


        public PromptItem(PromptItemDto promptItemDto)
        {
            MaxToken = promptItemDto.MaxToken;
            Temperature = promptItemDto.Temperature;
            TopP = promptItemDto.TopP;
            FrequencyPenalty = promptItemDto.FrequencyPenalty;
            NumsOfResults = promptItemDto.NumsOfResults;
            StopSequences = promptItemDto.StopSequences;
            ChatSystemPrompt = promptItemDto.ChatSystemPrompt;
            TokenSelectionBiases = promptItemDto.TokenSelectionBiases;
            EvaluationScore = promptItemDto.EvaluationScore;
            // Version = promptItemDto.Version;
            LastRunTime = promptItemDto.LastRunTime;
        }

        /// <summary>
        /// 新增时用的构造函数
        /// </summary>
        /// <param name="content"></param>
        /// <param name="modelId"></param>
        /// <param name="topP"></param>
        /// <param name="temperature"></param>
        /// <param name="maxToken"></param>
        /// <param name="frequencyPenalty"></param>
        /// <param name="presencePenalty"></param>
        /// <param name="stopSequences"></param>
        /// <param name="numsOfResults"></param>
        /// <param name="name"></param>
        /// <param name="tactic"></param>
        /// <param name="aiming"></param>
        /// <param name="parentTac"></param>
        public PromptItem(string content, int modelId, float topP, float temperature, int maxToken, float frequencyPenalty, float presencePenalty,
            string stopSequences, int numsOfResults, string name, string tactic, int aiming,string parentTac)
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
            Name = name;
            Tactic = tactic;
            Aiming = aiming;
            ParentTac = parentTac;
        }

        // [Obsolete]
        // /// <summary>
        // /// 获取当前版本的父版本
        // /// 例如：2021.08.01.1-1 的父版本为 2021.08.01.1
        // ///      2021.08.01.1-2 的父版本为 2021.08.01.1
        // ///      2021.08.01.1-1-1 的父版本为 2021.08.01.1-1
        // /// </summary>
        // /// <returns></returns>
        // [CanBeNull]
        // public string GetParentVersion()
        // {
        //     if (this.Version.Contains('-'))
        //     {
        //         // 去掉最后一个“-”及其后面的内容
        //         var index = this.Version.LastIndexOf('-');
        //         return this.Version.Substring(0, index);
        //     }
        //
        //     return null;
        // }

        public PromptItem Switch(bool show)
        {
            this.IsShare = show;

            return this;
        }

        public PromptItem ModifyName(string name)
        {
            this.Name = name;

            return this;
        }

        //public PromptItem SetDefaultName()
        //{
        //    if (string.IsNullOrWhiteSpace(Content))
        //    {
        //        Name = string.Empty;
        //        return this;
        //    }

        //    int maxLength = 
        //}
    }
}