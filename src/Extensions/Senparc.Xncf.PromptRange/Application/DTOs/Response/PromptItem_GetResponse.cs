using System;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class PromptItem_GetResponse : BaseResponse
    {
        /// <summary>
        /// Nick name
        /// </summary>
        public string NickName { get; set; }

        #region model config

        public int ModelId { get; set; }

        /// <summary>
        ///Maximum number of Tokens
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
        /// stop sequence (JSON array)
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        public string PromptContent { get; set; }

        /// <summary>
        ///Full version number
        /// </summary>
        public string FullVersion { get; set; }

        /// <summary>
        /// Note
        /// </summary>
        public string Note { get; set; }

        public new DateTime LastRunTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether to share
        /// </summary>
        public bool IsShare { get; set; } = false;

        public List<PromptResult> PromptResultList { get; set; } = new();

        #region 打分相关

        /// <summary>
        /// evaluation parameters, average score
        /// </summary>
        public decimal EvalAvgScore { get; set; }

        /// <summary>
        ///evaluation parameters
        /// </summary>
        public decimal EvalMaxScore { get; set; }

        /// <summary>
        ///Expected result Json
        /// </summary>
        public string ExpectedResultsJson { get; set; }

        #endregion


        #region Prompt请求参数

        /// <summary>
        /// prefix
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// suffix
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        ///parameter dictionary (JSON)
        /// </summary>
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