
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class PromptGroupDto : DtoBase
    {
        /// <summary>
        /// PromptGroup 名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// MaxToken
        /// </summary>
        public int MaxToken { get; private set; }

        /// <summary>
        /// Temperature
        /// </summary>
        public float Temperature { get; private set; }

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; private set; }

        /// <summary>
        /// FrequencyPenalty
        /// </summary>
        public float FrequencyPenalty { get; private set; }

        /// <summary>
        /// ResultsPerPrompt
        /// </summary>
        public int ResultsPerPrompt { get; private set; }

        /// <summary>
        /// StopSequences
        /// </summary>
        public string StopSequences { get; private set; }

        /// <summary>
        /// ChatSystemPrompt
        /// </summary>
        public string ChatSystemPrompt { get; private set; }

        /// <summary>
        /// TokenSelectionBiases
        /// </summary>
        public string TokenSelectionBiases { get; private set; }

        /// <summary>
        /// EvaluationMetrics
        /// </summary>
        public string EvaluationMetrics { get; private set; }

        private PromptGroupDto() { }
    }
}