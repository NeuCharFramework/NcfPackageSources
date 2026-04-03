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
        ///shooting range ID
        /// </summary>
        public int RangeId { get; set; }

        public PromptRangeDto PromptRange { get; set; }

        /// <summary>
        /// Nick name
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        ///Prompt content
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
        ///Maximum number of Tokens
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// frequency penalty
        /// </summary>
        public float FrequencyPenalty { get; set; }

        public float PresencePenalty { get; set; }

        /// <summary>
        /// stop sequence (JSON array) 
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        #region 打分

        /// <summary>
        /// evaluation parameters, average score
        /// </summary>
        public decimal EvalAvgScore { get; set; } = -1;

        /// <summary>
        ///evaluation parameters
        /// </summary>
        public decimal EvalMaxScore { get; set; } = -1;

        /// <summary>
        ///Expected result Json
        /// </summary>
        public string ExpectedResultsJson { get; set; }

        /// <summary>
        /// Whether to enable "ai scoring criteria"
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
        /// <para>is the number of target shooting, int</para>
        /// </summary>
        public int Aiming { get; set; }

        /// <summary>
        /// Parent Tactic, can be an empty string
        /// </summary>
        public string ParentTac { get; set; }

        #endregion

        /// <summary>
        ///Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        ///Last run time
        /// </summary>
        public DateTime LastRunTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Is it public?
        /// </summary>
        public bool IsShare { get; set; } = false;

        public bool IsDraft { get; set; }

        #region Prompt请求参数

        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public string VariableDictJson { get; set; }

        #endregion
    }
}