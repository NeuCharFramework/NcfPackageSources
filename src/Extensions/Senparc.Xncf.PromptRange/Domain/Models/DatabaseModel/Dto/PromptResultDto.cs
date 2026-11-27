/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultDto.cs
    File description: PromptResultDto related implementation

    Created by: Senparc - 20231021

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    /// <summary>Transfers prompt execution results and evaluation data to clients.</summary>
    public class PromptResultDto : DtoBase
    {
        /// <summary>Gets or sets the result identifier.</summary>
        public int Id { get; set; }

        #region LlmModel

        /// <summary>Gets or sets the language-model identifier.</summary>
        public int LlmModelId { get; set; }

        /// <summary>Gets or sets the related model entity.</summary>
        public LlModel LlModel { get; set; }

        #endregion

        #region Prompt Item

        /// <summary>Gets or sets the prompt item identifier.</summary>
        public int PromptItemId { get; set; }

        /// <summary>Gets or sets the prompt item version.</summary>
        public string PromptItemVersion { get; set; }

        #endregion


        /// <summary>Gets or sets the generated result text.</summary>
        public string ResultString { get; set; }

        /// <summary>Gets or sets execution time in milliseconds.</summary>
        public double CostTime { get; set; }

        /// <summary>Gets or sets the automated evaluation score.</summary>
        public decimal RobotScore { get; set; }

        /// <summary>Gets or sets the human evaluation score.</summary>
        public decimal HumanScore { get; set; }

        /// <summary>Gets or sets the final evaluation score.</summary>
        public decimal FinalScore { get; set; }


        /// <summary>Gets or sets the expected automated result text.</summary>
        public string RobotTestExceptedResult { get; set; }

        /// <summary>Gets or sets whether the robot result exactly matched expectations.</summary>
        public bool IsRobotTestExactlyEquat { get; set; }

        /// <summary>Gets or sets the evaluation modality.</summary>
        public TestType TestType { get; set; }

        /// <summary>Gets or sets prompt-input token cost.</summary>
        public int PromptCostToken { get; set; }

        /// <summary>Gets or sets generated-result token cost.</summary>
        public int ResultCostToken { get; set; }

        /// <summary>Gets or sets total token cost.</summary>
        public int TotalCostToken { get; set; }

        /// <summary>Gets or sets the result mode.</summary>
        public ResultMode? Mode { get; set; }

        /// <summary>Gets or sets the system message used for generation.</summary>
        public string SystemMessage { get; set; }
    }
}
