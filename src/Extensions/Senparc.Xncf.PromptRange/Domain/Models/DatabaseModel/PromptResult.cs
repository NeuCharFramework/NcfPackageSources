/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptResult.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20231021
    
    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResult))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class PromptResult : EntityBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int LlmModelId { get; private set; }

        // public LlmModel LlmModel { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ResultString { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public double CostTime { get; private set; }

        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal RobotScore { get; private set; } = -1;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal HumanScore { get; private set; } = -1;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal FinalScore { get; private set; } = -1;

        #endregion

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public int PromptItemId { get; private set; }

        /// <summary>
        /// PromptItemVersion 
        /// </summary>
        [MaxLength(50)]
        public string PromptItemVersion { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public ResultMode? Mode { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
            int robotScore, int humanScore, int finalScore, //Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult ManualScoring(decimal score)
        {
            HumanScore = score;

            return this;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult RobotScoring(decimal score)
        {
            RobotScore = score;

            return this;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public PromptResult FinalScoring(decimal score)
        {
            FinalScore = score;

            return this;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public PromptResult AppendUsageAndResult(int promptTokens, int completionTokens, int totalTokens, double costTime, string latestResult)
        {
            PromptCostToken += Math.Max(0, promptTokens);
            ResultCostToken += Math.Max(0, completionTokens);

            var safeTotal = Math.Max(0, totalTokens);
            if (safeTotal <= 0)
            {
                safeTotal = Math.Max(0, promptTokens) + Math.Max(0, completionTokens);
            }
            TotalCostToken += safeTotal;

            CostTime = costTime;
            if (!string.IsNullOrWhiteSpace(latestResult))
            {
                ResultString = latestResult;
            }
            return this;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum TestType
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Text,

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Graph,

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Voice
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum ResultMode
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Single = 1,

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Chat = 2
    }
}
