using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Models
{
    /// <summary>
    /// PromptResult 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResult))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class PromptResult : EntityBase<int>
    {
        /// <summary>
        /// PromptGroupId，并添加PromptGroup类作为属性
        /// </summary>

        public int PromptGroupId { get; private set; }
        //public PromptGroup PromptGroup { get; private set; }

        /// <summary>
        /// LlmModelId，并添加LlmModel类作为属性
        /// </summary>
        public int LlmModelId { get; private set; }
        public LlmModel LlmModel { get; private set; }

        /// <summary>
        /// 结果字符串
        /// </summary>
        public string ResultString { get; private set; }

        /// <summary>
        /// 花费时间，单位：毫秒
        /// </summary>
        public double CostTime { get; private set; }

        /// <summary>
        /// 机器人打分，0-100分
        /// </summary>
        public long RobotScore { get; private set; }

        /// <summary>
        /// 人类打分，0-100分
        /// </summary>
        public int HumanScore { get; private set; }

        /// <summary>
        /// RobotTestExceptedResult
        /// </summary>
        public string RobotTestExceptedResult { get; private set; }

        /// <summary>
        /// IsRobotTestExactlyEquat
        /// </summary>
        public bool IsRobotTestExactlyEquat { get; private set; }

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
        //public PromptItem PromptItem { get; private set; }

        public int PromptItemId { get; private set; }

        /// <summary>
        /// PromptItemVersion
        /// </summary>
        public string PromptItemVersion { get; private set; }

        private PromptResult() { }

        public PromptResult(//int promptGroupId,
            int llmModelId, string resultString, double costTime, int robotScore, int humanScore, string robotTestExceptedResult, bool isRobotTestExactlyEquat, TestType testType, int promptCostToken, int resultCostToken, int totalCostToken, string promptItemVersion, int promptItemId)
        {
            //PromptGroupId = promptGroupId;
            LlmModelId = llmModelId;
            ResultString = resultString;
            CostTime = costTime;
            RobotScore = robotScore;
            HumanScore = humanScore;
            RobotTestExceptedResult = robotTestExceptedResult;
            IsRobotTestExactlyEquat = isRobotTestExactlyEquat;
            TestType = testType;
            PromptCostToken = promptCostToken;
            ResultCostToken = resultCostToken;
            TotalCostToken = totalCostToken;
            PromptItemVersion = promptItemVersion;
            PromptItemId = promptItemId;
        }

        //public PromptResult(PromptResultDto promptResultDto)
        //{
        //    PromptGroupId = promptResultDto.PromptGroupId;
        //    //PromptGroup = promptResultDto.PromptGroup;
        //    LlmModelId = promptResultDto.LlmModelId;
        //    //LlmModel = promptResultDto.LlmModel;
        //    ResultString = promptResultDto.ResultString;
        //    CostTime = promptResultDto.CostTime;
        //    RobotScore = promptResultDto.RobotScore;
        //    HumanScore = promptResultDto.HumanScore;
        //    RobotTestExceptedResult = promptResultDto.RobotTestExceptedResult;
        //    IsRobotTestExactlyEquat = promptResultDto.IsRobotTestExactlyEquat;
        //    TestType = promptResultDto.TestType;
        //    PromptCostToken = promptResultDto.PromptCostToken;
        //    ResultCostToken = promptResultDto.ResultCostToken;
        //    TotalCostToken = promptResultDto.TotalCostToken;
        //    //PromptItem = promptResultDto.PromptItem;
        //    PromptItemVersion = promptResultDto.PromptItemVersion;
        //}

        public PromptResult Scoring(int score)
        {
            HumanScore = score;

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
