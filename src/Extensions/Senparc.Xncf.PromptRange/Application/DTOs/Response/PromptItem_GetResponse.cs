/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem_GetResponse.cs
    File description: PromptItem_GetResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    /// <summary>Returns a prompt item and its generated result history.</summary>
    public class PromptItem_GetResponse : BaseResponse
    {
        /// <summary>
        /// display name
        /// </summary>
        public string NickName { get; set; }

        #region model config

        /// <summary>Gets or sets the language-model identifier.</summary>
        public int ModelId { get; set; }

        /// <summary>
        /// maximum token count
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// temperature
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; set; }

        /// <summary>
        /// frequency penalty
        /// </summary>
        public float FrequencyPenalty { get; set; }

        /// <summary>Gets the presence penalty used during generation.</summary>
        public float PresencePenalty { get; private set; }

        /// <summary>Gets or sets serialized stop sequences.</summary>
        public string StopSequences { get; set; }

        #endregion

        /// <summary>Gets or sets the prompt content.</summary>
        public string PromptContent { get; set; }

        /// <summary>Gets or sets the full tactic/aiming version string.</summary>
        public string FullVersion { get; set; }

        /// <summary>
        /// Note
        /// </summary>
        public string Note { get; set; }

        /// <summary>Gets or sets the last execution timestamp.</summary>
        public new DateTime LastRunTime { get; set; } = DateTime.Now;

        /// <summary>Gets or sets whether the prompt is shared.</summary>
        public bool IsShare { get; set; } = false;

        /// <summary>Gets or sets generated prompt results.</summary>
        public List<PromptResult> PromptResultList { get; set; } = new();

        #region Legacy section

        /// <summary>Gets or sets the average evaluation score.</summary>
        public decimal EvalAvgScore { get; set; }

        /// <summary>Gets or sets the maximum evaluation score.</summary>
        public decimal EvalMaxScore { get; set; }

        /// <summary>Gets or sets serialized expected evaluation results.</summary>
        public string ExpectedResultsJson { get; set; }

        #endregion


        #region Legacy section

        /// <summary>Gets or sets the variable prefix.</summary>
        public string Prefix { get; set; }

        /// <summary>Gets or sets the variable suffix.</summary>
        public string Suffix { get; set; }

        /// <summary>Gets or sets serialized variable definitions.</summary>
        public string VariableDictJson { get; set; }

        #endregion

        public PromptItem_GetResponse(int promptItemId, string promptContent, string fullVersion, int modelId,
            int maxToken, float temperature, float topP, float frequencyPenalty, float presencePenalty, string stopSequences, string note,
            string prefix, string suffix, string variableDictJson, decimal evalAvgScore = -1, decimal evalMaxScore = -1,
            string expectedResultsJson = null)
        {
            Id = promptItemId;
            PromptContent = promptContent;
            FullVersion = fullVersion;
            ModelId = modelId;
            MaxToken = maxToken;
            Temperature = temperature;
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
            StopSequences = stopSequences;
            Note = note;
            Prefix = prefix;
            Suffix = suffix;
            VariableDictJson = variableDictJson;
            EvalAvgScore = evalAvgScore;
            EvalMaxScore = evalMaxScore;
            ExpectedResultsJson = expectedResultsJson;
        }

        public PromptItem_GetResponse(PromptItemDto item) : this(item.Id, item.Content, item.FullVersion, item.ModelId,
            item.MaxToken, item.Temperature, item.TopP, item.FrequencyPenalty, item.PresencePenalty, item.StopSequences, item.Note,
            item.Prefix, item.Suffix, item.VariableDictJson, item.EvalAvgScore, item.EvalMaxScore, item.ExpectedResultsJson)
        {
        }
    }
}
