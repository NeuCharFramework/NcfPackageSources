/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptResultDto.cs
    File description: PromptResultDto related implementation
    
    
    Created by: Senparc - 20231021
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class PromptResultDto : DtoBase
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Id { get; set; }

        #region LlmModel

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int LlmModelId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public LlModel LlModel { get; set; }

        #endregion

        #region Prompt Item

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int PromptItemId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string PromptItemVersion { get; set; }

        #endregion


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ResultString { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public double CostTime { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal RobotScore { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal HumanScore { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal FinalScore { get; set; }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string RobotTestExceptedResult { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsRobotTestExactlyEquat { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public TestType TestType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int PromptCostToken { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int ResultCostToken { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int TotalCostToken { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public ResultMode? Mode { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public string SystemMessage { get; set; }
    }
}