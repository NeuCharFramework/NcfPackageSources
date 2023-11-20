using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// PromptItem
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptItem))]/*必须添加前缀，防止全系统中发生冲突*/
    [Serializable]
    public class PromptItem : EntityBase<int>
    {
        public string Name { get; private set; }

        /// <summary>
        /// Prompt内容
        /// </summary>
        public string Content { get; private set; }

        [Required]
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
        public string StopSequences { get; private set; }
        #endregion

        /// <summary>
        /// 每个 Prompt 的结果数
        /// </summary>
        public int NumsOfResults { get; private set; } = 0;


        /// <summary>
        /// 聊天系统 Prompt
        /// </summary>
        public string ChatSystemPrompt { get; private set; }

        /// <summary>
        /// Token 选择偏好
        /// </summary>
        public string TokenSelectionBiases { get; private set; }

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvaluationScore { get; private set; }

        /// <summary>
        /// 版本号，格式为 yyyy.MM.dd.Build
        /// </summary>
        [Required]
        public string Version { get; private set; }

        /// <summary>
        /// 最后一次运行时间
        /// </summary>
        public DateTime LastRunTime { get; private set; }

        [Required]
        public bool Show { get; private set; }

        private PromptItem() { }

        public PromptItem(string name, string content, int modelId, float topP, float temperature, int maxToken, float frequencyPenalty, float presencePenalty, string stopSequences, int numsOfResults, string chatSystemPrompt, string tokenSelectionBiases, int evaluationScore, string version, DateTime lastRunTime)
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
            Version = version;
            LastRunTime = lastRunTime;
        }


        public PromptItem(PromptItemDto promptItemDto)
        {
            MaxToken = promptItemDto.MaxToken;
            Temperature = promptItemDto.Temperature;
            TopP = promptItemDto.TopP;
            FrequencyPenalty = promptItemDto.FrequencyPenalty;
            NumsOfResults = promptItemDto.ResultsPerPrompt;
            StopSequences = promptItemDto.StopSequences;
            ChatSystemPrompt = promptItemDto.ChatSystemPrompt;
            TokenSelectionBiases = promptItemDto.TokenSelectionBiases;
            EvaluationScore = promptItemDto.EvaluationScore;
            Version = promptItemDto.Version;
            LastRunTime = promptItemDto.LastRunTime;
        }

        ///// <summary>
        ///// 获取版本信息
        ///// </summary>
        ///// <param name="version">如果留空则默认获取当前示例的 Version</param>
        ///// <returns></returns>
        //public VersionInfo GetVersionInfo(string version = null)
        //{
        //    // TODO: 正则快还是split快
        //    string regexPattern = @"(\d+)\.(\d+)\.(\d+)\.(\d+)";
        //    Match match = Regex.Match(version, regexPattern);
        //    if (match.Success)
        //    {
        //        int ConverToInt(string str) => int.Parse(str);

        //        int major = ConverToInt(match.Groups[1].Value);
        //        int minor = ConverToInt(match.Groups[2].Value);
        //        int patch = ConverToInt(match.Groups[3].Value);
        //        int build = ConverToInt(match.Groups[4].Value);

        //        return new VersionInfo(major, minor, patch, build);
        //    }
        //    else
        //    {
        //        return new VersionInfo(0, 0, 0, 0);
        //    }
        //}

        ///// <summary>
        ///// 生成新的版本号
        ///// </summary>
        ///// <param name="lastVersion">上一个版本，如果为 null，则使用当前实例的 Version</param>
        ///// <returns></returns>
        //public VersionInfo GenerateNewVersion(string lastVersion = null)
        //{
        //    var today = SystemTime.Now;

        //    if (string.IsNullOrWhiteSpace(lastVersion))
        //    {
        //        return new VersionInfo(today.Year, today.Month, today.Day, 1);
        //    }
        //    var lastVersionInfo = GetVersionInfo(lastVersion);
        //    lastVersionInfo.Build++;
        //    return lastVersionInfo;

        //    //lastVersion ??= Version ?? "";

        //    //var today = SystemTime.Now;
        //    //int major = today.Year;
        //    //int minor = today.Month;
        //    //int patch = today.Day;

        //    //var lastVersionInfo = GetVersionInfo(lastVersion);
        //    //if (lastVersionInfo.Major != 0 &&
        //    //    lastVersionInfo.Major == major &&
        //    //    lastVersionInfo.Minor == minor &&
        //    //    lastVersionInfo.Patch == patch)
        //    //{
        //    //    //当天版本，Build 号加 1
        //    //    lastVersionInfo.Build++;
        //    //    return lastVersionInfo;
        //    //}
        //    //else
        //    //{
        //    //    //返回当天第一个版本
        //    //    var versionInfo = new VersionInfo(major, minor, patch, 1);
        //    //    return versionInfo;
        //    //}
        //}

        /// <summary>
        /// 更新版本号
        /// 
        /// </summary>
        public void UpdateVersion(string newVersion)
        {
            //todo: validate
            this.Version = newVersion;
        }

        public PromptItem Switch(bool show)
        {
            this.Show = show;

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
