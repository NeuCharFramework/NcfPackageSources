using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Models
{
    /// <summary>
    /// PromptResult 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResult))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class PromptResult : EntityBase<int>
    {
        /// <summary>
        /// LlmModelId，并添加LlmModel类作为属性
        /// </summary>
        public int LlmModelId { get; private set; }

        // public LlmModel LlmModel { get; private set; }

        /// <summary>
        /// 结果字符串
        /// </summary>
        public string ResultString { get; private set; }

        /// <summary>
        /// 花费时间，单位：毫秒
        /// </summary>
        public double CostTime { get; private set; }

        /// <summary>
        /// 机器人打分，0-10分
        /// </summary>
        public int RobotScore { get; private set; }

        /// <summary>
        /// 人类打分，0-10分
        /// </summary>
        public int HumanScore { get; private set; }

        /// <summary>
        /// 最终得分
        /// </summary>
        public int FinalScore { get; private set; }

        // /// <summary>
        // /// RobotTestExceptedResult 自动打分期望值
        // /// </summary>
        // public string RobotTestExceptedResult { get; private set; }

        // /// <summary>
        // /// IsRobotTestExactlyEquat
        // /// </summary>
        // public bool IsRobotTestExactlyEquat { get; private set; }

        /// <summary>
        /// 测试类型，枚举中包含：文字、图形、声音
        /// </summary>
        public TestType TestType { get; private set; }

        /// <summary>
        /// PromptCostToken
        /// </summary>
        public int PromptCostToken { get; private set; }

        /// <summary>
        /// ResultCostToken
        /// </summary>
        public int ResultCostToken { get; private set; }

        /// <summary>
        /// TotalCostToken
        /// </summary>
        public int TotalCostToken { get; private set; }

        /// <summary>
        /// PromptItem，并添加PromptItem类作为属性
        /// </summary>
        public int PromptItemId { get; private set; }

        /// <summary>
        /// PromptItemVersion 
        /// </summary>
        [MaxLength(50)]
        public string PromptItemVersion { get; private set; }

        private PromptResult()
        {
        }

        public PromptResult(PromptResultDto dto)
        {
            LlmModelId = dto.LlmModelId;
            ResultString = dto.ResultString;
            CostTime = dto.CostTime;
            RobotScore = dto.RobotScore;
            HumanScore = dto.HumanScore;
            TestType = dto.TestType;
            PromptCostToken = dto.PromptCostToken;
            ResultCostToken = dto.ResultCostToken;
            TotalCostToken = dto.TotalCostToken;
            PromptItemVersion = dto.PromptItemVersion;
            PromptItemId = dto.PromptItemId;
        }


        public PromptResult(
            int llmModelId, string resultString, double costTime, int robotScore, int humanScore,
            string robotTestExceptedResult, bool isRobotTestExactlyEquat, TestType testType, int promptCostToken,
            int resultCostToken, int totalCostToken, string promptItemVersion, int promptItemId)
        {
            LlmModelId = llmModelId;
            ResultString = resultString;
            CostTime = costTime;
            RobotScore = robotScore;
            HumanScore = humanScore;
            TestType = testType;
            PromptCostToken = promptCostToken;
            ResultCostToken = resultCostToken;
            TotalCostToken = totalCostToken;
            PromptItemVersion = promptItemVersion;
            PromptItemId = promptItemId;
        }


        /// <summary>
        /// 更新手动评分
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult ManualScoring(int score)
        {
            HumanScore = score;

            return this;
        }

        /// <summary>
        /// 更新自动机器评分
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult RobotScoring(int score)
        {
            RobotScore = score;

            return this;
        }

        /// <summary>
        /// 更新最终得分
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult FinalScoring(int score)
        {
            FinalScore = score;

            return this;
        }
    }

    /// <summary>
    /// 测试类型枚举
    /// </summary>
    public enum TestType
    {
        /// <summary>
        /// 文字
        /// </summary>
        Text,

        /// <summary>
        /// 图形
        /// </summary>
        Graph,

        /// <summary>
        /// 声音
        /// </summary>
        Voice
    }
}