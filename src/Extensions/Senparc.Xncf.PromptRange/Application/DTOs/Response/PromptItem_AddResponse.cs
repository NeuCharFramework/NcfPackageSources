/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem_AddResponse.cs
    File description: PromptItem_AddResponse related implementation

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

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    /// <summary>Returns the persisted prompt-item configuration and generated results.</summary>
    public class PromptItem_AddResponse : BaseResponse
    {
        /// <summary>
        /// range　ID
        /// </summary>
        public int RangeId { get; set; }

        /// <summary>
        /// display name
        /// </summary>
        public string NickName { get; set; }

        /// <summary>Gets or sets the persisted prompt content.</summary>
        public string PromptContent { get; set; }


        /// <summary>Gets or sets the full tactic/aiming version string.</summary>
        public string FullVersion { get; set; }

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

        /// <summary>
        /// Note
        /// </summary>
        public string Note { get; set; }

        /// <summary>Gets or sets the last execution timestamp.</summary>
        public new DateTime LastRunTime { get; set; } = DateTime.Now;

        /// <summary>Gets or sets whether the prompt is a draft.</summary>
        public bool IsDraft { get; set; }
        /// <summary>Gets or sets whether the prompt is shared.</summary>
        public bool IsShare { get; set; }

        /// <summary>Gets or sets generated prompt results.</summary>
        public List<PromptResultDto> PromptResultList { get; set; } = new();

        #region Legacy section

        /// <summary>Gets or sets the average evaluation score.</summary>
        public decimal EvalAvgScore { get; set; }

        /// <summary>Gets or sets the maximum evaluation score.</summary>
        public decimal EvalMaxScore { get; set; }

        /// <summary>Gets or sets serialized expected evaluation results.</summary>
        public string ExpectedResultsJson { get; set; }

        #endregion

        /// <summary>Gets or sets the variable prefix.</summary>
        public string Prefix { get; set; }
        /// <summary>Gets or sets the variable suffix.</summary>
        public string Suffix { get; set; }
        /// <summary>Gets or sets serialized variable definitions.</summary>
        public string VariableDictJson { get; set; }

        /// <summary>
        /// Initializes a complete prompt-item response projection.
        /// </summary>
        /// <param name="promptItemId">The prompt item identifier.</param>
        /// <param name="promptContent">The prompt content.</param>
        /// <param name="fullVersion">The prompt version.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="maxToken">The maximum token setting.</param>
        /// <param name="temperature">The temperature setting.</param>
        /// <param name="topP">The TopP setting.</param>
        /// <param name="frequencyPenalty">The frequency penalty.</param>
        /// <param name="presencePenalty">The presence penalty.</param>
        /// <param name="stopSequences">The serialized stop sequences.</param>
        /// <param name="note">The prompt note.</param>
        /// <param name="prefix">The variable prefix.</param>
        /// <param name="suffix">The variable suffix.</param>
        /// <param name="variableDictJson">The variable dictionary JSON.</param>
        /// <param name="evalAvgScore">The average score.</param>
        /// <param name="evalMaxScore">The maximum score.</param>
        /// <param name="expectedResultsJson">The expected results JSON.</param>
        /// <param name="isDraft">Whether the prompt is a draft.</param>
        /// <param name="isShare">Whether the prompt is shared.</param>
        /// <param name="nickName">The display nickname.</param>
        /// <param name="rangeId">The prompt range identifier.</param>
        public PromptItem_AddResponse(int promptItemId, string promptContent, string fullVersion, int modelId,
            int maxToken, float temperature, float topP, float frequencyPenalty, float presencePenalty, string stopSequences, string note,
            string prefix, string suffix, string variableDictJson, decimal evalAvgScore, decimal evalMaxScore, string expectedResultsJson,
            bool isDraft, bool isShare, string nickName, int rangeId)
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
            IsDraft = isDraft;
            IsShare = isShare;
            NickName = nickName;
            RangeId = rangeId;
        }

        /// <summary>
        /// Initializes a response from a PromptItem entity.
        /// </summary>
        /// <param name="item">The source prompt item.</param>
        public PromptItem_AddResponse(PromptItem item) : this(item.Id, item.Content, item.FullVersion, item.ModelId,
            item.MaxToken, item.Temperature, item.TopP, item.FrequencyPenalty, item.PresencePenalty, item.StopSequences, item.Note,
            item.Prefix, item.Suffix, item.VariableDictJson, item.EvalAvgScore, item.EvalMaxScore, item.ExpectedResultsJson,
            item.IsDraft, item.IsShare, item.NickName, item.RangeId)
        {
        }

        /// <summary>
        /// Initializes a response from a PromptItem DTO.
        /// </summary>
        /// <param name="dto">The source DTO.</param>
        public PromptItem_AddResponse(PromptItemDto dto) : this(dto.Id, dto.Content, dto.FullVersion, dto.ModelId,
            dto.MaxToken, dto.Temperature, dto.TopP, dto.FrequencyPenalty, dto.PresencePenalty, dto.StopSequences, dto.Note,
            dto.Prefix, dto.Suffix, dto.VariableDictJson, dto.EvalAvgScore, dto.EvalMaxScore, dto.ExpectedResultsJson,
            dto.IsDraft, dto.IsShare, dto.NickName, dto.RangeId)
        {
        }
    }
}
