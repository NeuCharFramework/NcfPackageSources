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
    public class PromptItemDto : DtoBase
    {
        public int Id { get; set; }

        /// <summary>
        /// range　ID
        /// </summary>
        public int RangeId { get; set; }

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

        public int ModelId { get; set; }

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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal EvalAvgScore { get; set; } = -1;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal EvalMaxScore { get; set; } = -1;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ExpectedResultsJson { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool isAIGrade { get; set; } = false;

        #endregion


        #region Full Version

        public string FullVersion
        {
            get { return $"{RangeName}-T{Tactic}-A{Aiming}"; }
            set { }
        }

        public string RangeName { get; set; }

        public string Tactic { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Aiming { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ParentTac { get; set; }

        #endregion

        /// <summary>
        /// Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime LastRunTime { get; set; } = DateTime.Now;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsShare { get; set; } = false;

        public bool IsDraft { get; set; }

        #region Legacy section

        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public string VariableDictJson { get; set; }

        #endregion
    }
}