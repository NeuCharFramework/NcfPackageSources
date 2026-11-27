/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult.cs
    File description: Related implementation.

    Created by: Senparc - 20231021

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    /// <summary>
    /// Stores one prompt execution result, usage totals, timing, scores, and mode metadata.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResult))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class PromptResult : EntityBase<int>
    {
        /// <summary>Gets the language-model configuration identifier.</summary>
        public int LlmModelId { get; private set; }

        // public LlmModel LlmModel { get; private set; }

        /// <summary>Gets the generated result text.</summary>
        public string ResultString { get; private set; }

        /// <summary>Gets execution time in milliseconds.</summary>
        public double CostTime { get; private set; }

        #region Legacy section

        /// <summary>Gets the automated evaluation score.</summary>
        public decimal RobotScore { get; private set; } = -1;

        /// <summary>Gets the human evaluation score.</summary>
        public decimal HumanScore { get; private set; } = -1;

        /// <summary>Gets the final evaluation score.</summary>
        public decimal FinalScore { get; private set; } = -1;

        #endregion

        /// <summary>Gets the evaluation modality.</summary>
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

        /// <summary>Gets the prompt item identifier.</summary>
        public int PromptItemId { get; private set; }

        /// <summary>
        /// PromptItemVersion 
        /// </summary>
        [MaxLength(50)]
        public string PromptItemVersion { get; private set; }

        /// <summary>Gets the result mode.</summary>
        public ResultMode? Mode { get; private set; }

        /// <summary>Gets the system message supplied to the model.</summary>
        public string SystemMessage { get; private set; }

        private PromptResult()
        {
        }

        /// <summary>Creates a result entity from a DTO.</summary>
        /// <param name="dto">The result values to copy.</param>
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


        /// <summary>Sets the human evaluation score.</summary>
        /// <param name="score">The human-provided score.</param>
        /// <returns>This result entity.</returns>
        public PromptResult ManualScoring(decimal score)
        {
            HumanScore = score;

            return this;
        }

        /// <summary>Sets the automated evaluation score.</summary>
        /// <param name="score">The automated score.</param>
        /// <returns>This result entity.</returns>
        public PromptResult RobotScoring(decimal score)
        {
            RobotScore = score;

            return this;
        }

        /// <summary>Sets the final evaluation score.</summary>
        /// <param name="score">The final score.</param>
        /// <returns>This result entity.</returns>
        public PromptResult FinalScoring(decimal score)
        {
            FinalScore = score;

            return this;
        }

        /// <summary>
        /// Adds usage counters and optionally replaces the latest result text.
        /// </summary>
        /// <param name="promptTokens">Additional prompt tokens.</param>
        /// <param name="completionTokens">Additional completion tokens.</param>
        /// <param name="totalTokens">Reported total tokens; recalculated when non-positive.</param>
        /// <param name="costTime">Latest execution time in milliseconds.</param>
        /// <param name="latestResult">Optional latest generated result text.</param>
        /// <returns>This result entity.</returns>
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

    /// <summary>Identifies the evaluation modality.</summary>
    public enum TestType
    {
        /// <summary>A text-based evaluation.</summary>
        Text,

        /// <summary>A graph or visual evaluation.</summary>
        Graph,

        /// <summary>A voice evaluation.</summary>
        Voice
    }

    /// <summary>Identifies how a prompt result is presented or continued.</summary>
    public enum ResultMode
    {
        /// <summary>A single prompt response.</summary>
        Single = 1,

        /// <summary>A multi-turn chat response.</summary>
        Chat = 2
    }
}
