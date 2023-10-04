using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        /// 停止序列
        /// </summary>
        public string[] StopSequences { get; private set; }

        /// <summary>
        /// 聊天系统 Prompt
        /// </summary>
        public string ChatSystemPrompt { get; private set; }

        /// <summary>
        /// Token 选择偏好
        /// </summary>
        public float[] TokenSelectionBiases { get; private set; }

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvaluationScore { get; private set; }

        /// <summary>
        /// 版本号，格式为 yyyy.MM.dd.Version
        /// </summary>
        public string Version { get; private set; }


        /// <summary>
        /// 最后一次运行时间
        /// </summary>
        public DateTime LastRunTime { get; private set; }

        public PromptGroup PromptGroup { get; private set; }

        private PromptItem() { }

        public PromptItem(int promptGroupId, int maxToken, float temperature, float topP, float frequencyPenalty, int resultsPerPrompt, string[] stopSequences, string chatSystemPrompt, float[] tokenSelectionBiases, int evaluationScore, string version, DateTime lastRunTime)
        {
            PromptGroupId = promptGroupId;
            MaxToken = maxToken;
            Temperature = temperature;
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            ResultsPerPrompt = resultsPerPrompt;
            StopSequences = stopSequences;
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
            ResultsPerPrompt = promptItemDto.ResultsPerPrompt;
            StopSequences = promptItemDto.StopSequences;
            ChatSystemPrompt = promptItemDto.ChatSystemPrompt;
            TokenSelectionBiases = promptItemDto.TokenSelectionBiases;
            EvaluationScore = promptItemDto.EvaluationScore;
            Version = promptItemDto.Version;
            LastRunTime = promptItemDto.LastRunTime;
        }

        /// <summary>
        /// 获取版本信息
        /// </summary>
        /// <param name="version">如果留空则默认获取当前示例的 Version</param>
        /// <returns></returns>
        public VersionInfo GetVersionInfo(string version = null)
        {
            version ??= Version ?? "";

            string regexPattern = @"(\d+)\.(\d+)\.(\d+)\.(\d+)";
            Match match = Regex.Match(version, regexPattern);
            if (match.Success)
            {
                int ConverToInt(string str) => int.Parse(str);

                int major = ConverToInt(match.Groups[1].Value);
                int minor = ConverToInt(match.Groups[2].Value);
                int patch = ConverToInt(match.Groups[3].Value);
                int build = ConverToInt(match.Groups[4].Value);

                return new VersionInfo(major, minor, patch, build);
            }
            else
            {
                return new VersionInfo(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// 生成新的版本号
        /// </summary>
        /// <param name="lastVersion"></param>
        /// <returns></returns>
        public VersionInfo GenerateNewVersion(string lastVersion)
        {
            var today = SystemTime.Now;
            int major = today.Year;
            int minor = today.Month;
            int patch = today.Day;

            var lastVersionInfo = GetVersionInfo(lastVersion);
            if (lastVersionInfo.Major != 0 &&
                lastVersionInfo.Major == major &&
                lastVersionInfo.Minor == minor &&
                lastVersionInfo.Patch == patch)
            {
                //当天版本，Build 号加 1
                lastVersionInfo.Build++;
                return lastVersionInfo;
            }
            else
            {
                //返回当天第一个版本
                var versionInfo = new VersionInfo(major, minor, patch, 1);
                return versionInfo;
            }
        }
    }
}
