/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptItem_AddResponse.cs
    File description: PromptItem_AddResponse related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
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

        public string PromptContent { get; set; }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string FullVersion { get; set; }

        #region model config

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

        public float PresencePenalty { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        /// <summary>
        /// Note
        /// </summary>
        public string Note { get; set; }

        public new DateTime LastRunTime { get; set; } = DateTime.Now;

        public bool IsDraft { get; set; }
        public bool IsShare { get; set; }

        public List<PromptResultDto> PromptResultList { get; set; } = new();

        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal EvalAvgScore { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal EvalMaxScore { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ExpectedResultsJson { get; set; }

        #endregion

        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string VariableDictJson { get; set; }

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

        public PromptItem_AddResponse(PromptItem item) : this(item.Id, item.Content, item.FullVersion, item.ModelId,
            item.MaxToken, item.Temperature, item.TopP, item.FrequencyPenalty, item.PresencePenalty, item.StopSequences, item.Note,
            item.Prefix, item.Suffix, item.VariableDictJson, item.EvalAvgScore, item.EvalMaxScore, item.ExpectedResultsJson,
            item.IsDraft, item.IsShare, item.NickName, item.RangeId)
        {
        }

        public PromptItem_AddResponse(PromptItemDto dto) : this(dto.Id, dto.Content, dto.FullVersion, dto.ModelId,
            dto.MaxToken, dto.Temperature, dto.TopP, dto.FrequencyPenalty, dto.PresencePenalty, dto.StopSequences, dto.Note,
            dto.Prefix, dto.Suffix, dto.VariableDictJson, dto.EvalAvgScore, dto.EvalMaxScore, dto.ExpectedResultsJson,
            dto.IsDraft, dto.IsShare, dto.NickName, dto.RangeId)
        {
        }
    }
}