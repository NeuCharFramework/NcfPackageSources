//using Senparc.Ncf.Core.Models;
//using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace Senparc.Xncf.PromptRange
//{
//    /// <summary>
//    /// PromptGroup
//    /// </summary>
//    [Table(Register.DATABASE_PREFIX + nameof(PromptGroup))]/*必须添加前缀，防止全系统中发生冲突*/
//    [Serializable]
//    public class PromptGroup : EntityBase<int>
//    {
//        ///// <summary>
//        ///// PromptGroup 名称
//        ///// </summary>
//        //public string Name { get; private set; }

//        ///// <summary>
//        ///// MaxToken
//        ///// </summary>
//        //public int MaxToken { get; private set; }

//        ///// <summary>
//        ///// Temperature
//        ///// </summary>
//        //public float Temperature { get; private set; }

//        ///// <summary>
//        ///// TopP
//        ///// </summary>
//        //public float TopP { get; private set; }

//        ///// <summary>
//        ///// FrequencyPenalty
//        ///// </summary>
//        //public float FrequencyPenalty { get; private set; }

//        ///// <summary>
//        ///// ResultsPerPrompt
//        ///// </summary>
//        //public int ResultsPerPrompt { get; private set; }

//        ///// <summary>
//        ///// StopSequences
//        ///// </summary>
//        //public string StopSequences { get; private set; }

//        ///// <summary>
//        ///// ChatSystemPrompt
//        ///// </summary>
//        //public string ChatSystemPrompt { get; private set; }

//        ///// <summary>
//        ///// TokenSelectionBiases
//        ///// </summary>
//        //public string TokenSelectionBiases { get; private set; }

//        ///// <summary>
//        ///// EvaluationMetrics
//        ///// </summary>
//        //public string EvaluationMetrics { get; private set; }


//        //public List<PromptItem> PromptItems { get; set; } = new List<PromptItem>();

//        //private PromptGroup() { }

//        //public PromptGroup(PromptGroupDto promptGroupDto)
//        //{
//        //    Name = promptGroupDto.Name;
//        //    MaxToken = promptGroupDto.MaxToken;
//        //    Temperature = promptGroupDto.Temperature;
//        //    TopP = promptGroupDto.TopP;
//        //    FrequencyPenalty = promptGroupDto.FrequencyPenalty;
//        //    ResultsPerPrompt = promptGroupDto.ResultsPerPrompt;
//        //    StopSequences = promptGroupDto.StopSequences;
//        //    ChatSystemPrompt = promptGroupDto.ChatSystemPrompt;
//        //    TokenSelectionBiases = promptGroupDto.TokenSelectionBiases;
//        //    EvaluationMetrics = promptGroupDto.EvaluationMetrics;
//        //}

//        //public PromptGroup(string name, int maxToken, float temperature, float topP, float frequencyPenalty, int resultsPerPrompt, string stopSequences, string chatSystemPrompt, string tokenSelectionBiases, string evaluationMetrics)
//        //{
//        //    Name = name;
//        //    MaxToken = maxToken;
//        //    Temperature = temperature;
//        //    TopP = topP;
//        //    FrequencyPenalty = frequencyPenalty;
//        //    ResultsPerPrompt = resultsPerPrompt;
//        //    StopSequences = stopSequences;
//        //    ChatSystemPrompt = chatSystemPrompt;
//        //    TokenSelectionBiases = tokenSelectionBiases;
//        //    EvaluationMetrics = evaluationMetrics;
//        //}
//    }
//}

