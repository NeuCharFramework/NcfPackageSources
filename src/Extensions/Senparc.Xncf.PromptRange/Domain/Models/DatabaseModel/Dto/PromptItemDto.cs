/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItemDto.cs
    File description: PromptItemDto related implementation

    Created by: Senparc - 20231003

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    /// <summary>Transfers one PromptRange prompt item and its execution/evaluation settings.</summary>
    public class PromptItemDto : DtoBase
    {
        /// <summary>Gets or sets the prompt-item identifier.</summary>
        public int Id { get; set; }

        /// <summary>
        /// range　ID
        /// </summary>
        public int RangeId { get; set; }

        /// <summary>Gets or sets the owning PromptRange DTO.</summary>
        public PromptRangeDto PromptRange { get; set; }

        /// <summary>
        /// display name
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// Promptcontent
        /// </summary>
        public string Content { get; set; }

        #region Model Config

        /// <summary>Gets or sets the AI model identifier.</summary>
        public int ModelId { get; set; }

        /// <summary>Gets or sets the mapped AI model DTO.</summary>
        public AIModelDto AIModelDto { get; set; }

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; set; }

        /// <summary>
        /// temperature
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// maximum token count
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// frequency penalty
        /// </summary>
        public float FrequencyPenalty { get; set; }

        public float PresencePenalty { get; set; }

        /// <summary>Gets or sets serialized stop sequences.</summary>
        public string StopSequences { get; set; }

        #endregion

        #region Legacy section

        /// <summary>Gets or sets the average evaluation score.</summary>
        public decimal EvalAvgScore { get; set; } = -1;

        /// <summary>Gets or sets the maximum evaluation score.</summary>
        public decimal EvalMaxScore { get; set; } = -1;

        /// <summary>Gets or sets serialized expected evaluation results.</summary>
        public string ExpectedResultsJson { get; set; }

        /// <summary>Gets or sets whether automatic grading is enabled.</summary>
        public bool isAIGrade { get; set; } = false;

        #endregion


        #region Full Version

        /// <summary>Gets the composed range/tactic/aiming version string.</summary>
        public string FullVersion
        {
            get { return $"{RangeName}-T{Tactic}-A{Aiming}"; }
            set { }
        }

        /// <summary>Gets or sets the range display name.</summary>
        public string RangeName { get; set; }

        /// <summary>Gets or sets the tactic segment.</summary>
        public string Tactic { get; set; }

        /// <summary>Gets or sets the aiming segment.</summary>
        public int Aiming { get; set; }

        /// <summary>Gets or sets the parent tactic segment.</summary>
        public string ParentTac { get; set; }

        #endregion

        /// <summary>
        /// Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>Gets or sets the last execution timestamp.</summary>
        public DateTime LastRunTime { get; set; } = DateTime.Now;

        /// <summary>Gets or sets whether the item is shared.</summary>
        public bool IsShare { get; set; } = false;

        /// <summary>Gets or sets whether the item is a draft.</summary>
        public bool IsDraft { get; set; }

        #region Legacy section

        /// <summary>Gets or sets the variable prefix.</summary>
        public string Prefix { get; set; }
        /// <summary>Gets or sets the variable suffix.</summary>
        public string Suffix { get; set; }

        /// <summary>Gets or sets serialized variable definitions.</summary>
        public string VariableDictJson { get; set; }

        #endregion
    }
}
