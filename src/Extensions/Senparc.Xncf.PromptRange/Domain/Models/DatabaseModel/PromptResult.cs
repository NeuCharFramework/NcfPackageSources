using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    /// <summary>
    /// PromptResult: PromptItem’s target shooting result
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResult))] //The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class PromptResult : EntityBase<int>
    {
        /// <summary>
        ///LlmModelId and add the LlmModel class as a property
        /// </summary>
        public int LlmModelId { get; private set; }

        // public LlmModel LlmModel { get; private set; }

        /// <summary>
        ///result string
        /// </summary>
        public string ResultString { get; private set; }

        /// <summary>
        /// Time spent, unit: milliseconds
        /// </summary>
        public double CostTime { get; private set; }

        #region 打分

        /// <summary>
        /// Robot scoring, 0-10 points
        /// </summary>
        public decimal RobotScore { get; private set; } = -1;

        /// <summary>
        /// Human rating, 0-10 points
        /// </summary>
        public decimal HumanScore { get; private set; } = -1;

        /// <summary>
        /// final score
        /// </summary>
        public decimal FinalScore { get; private set; } = -1;

        #endregion

        /// <summary>
        /// Test type, the enumeration includes: text, graphics, sound
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
        ///PromptItem and add PromptItem class as attribute
        /// </summary>
        public int PromptItemId { get; private set; }

        /// <summary>
        /// PromptItemVersion 
        /// </summary>
        [MaxLength(50)]
        public string PromptItemVersion { get; private set; }

        /// <summary>
        /// Target practice mode: Chat (chat mode) or Single (single test mode), can be empty (compatible with old data)
        /// </summary>
        public ResultMode? Mode { get; private set; }

        /// <summary>
        /// SystemMessage (Prompt content, final content after completing parameter replacement)
        /// Used in conversation mode to ensure that even if the Prompt content or parameters change, the historically used SystemMessage can be traced
        /// </summary>
        public string SystemMessage { get; private set; }

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
            Mode = dto.Mode;
            SystemMessage = dto.SystemMessage;
        }


        public PromptResult(
            int llmModelId, string resultString, double costTime,
            int robotScore, int humanScore, int finalScore, // Fraction
            TestType testType, int promptCostToken,
            int resultCostToken, int totalCostToken, string promptItemVersion, int promptItemId,
            ResultMode? mode = null, string systemMessage = null)
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
            Mode = mode;
            SystemMessage = systemMessage;
        }


        /// <summary>
        ///update manual rating
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult ManualScoring(decimal score)
        {
            HumanScore = score;

            return this;
        }

        /// <summary>
        ///Update automatic machine ratings
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult RobotScoring(decimal score)
        {
            RobotScore = score;

            return this;
        }

        /// <summary>
        ///update final score
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult FinalScoring(decimal score)
        {
            FinalScore = score;

            return this;
        }
    }

    /// <summary>
    ///Test type enum
    /// </summary>
    public enum TestType
    {
        /// <summary>
        /// Word
        /// </summary>
        Text,

        /// <summary>
        ///graphics
        /// </summary>
        Graph,

        /// <summary>
        /// sound
        /// </summary>
        Voice
    }

    /// <summary>
    /// Targeting mode enumeration
    /// </summary>
    public enum ResultMode
    {
        /// <summary>
        ///Single test mode
        /// </summary>
        Single = 1,

        /// <summary>
        ///chat mode
        /// </summary>
        Chat = 2
    }
}