using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class PromptItemDto : DtoBase
    {
        /// <summary>
        /// PromptGroupId
        /// </summary>
        public int PromptGroupId { get; private set; }

        /// <summary>
        /// 最大 Token 数
        /// </summary>
        public int MaxToken { get; private set; }

        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature { get; private set; }

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; private set; }

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public float FrequencyPenalty { get; private set; }

        /// <summary>
        /// 每个 Prompt 的结果数
        /// </summary>
        public int ResultsPerPrompt { get; private set; }

        /// <summary>
        /// 停止序列
        /// </summary>
        public string[] StopSequences { get; private set; }

        /// <summary>
        /// 聊天系统 Prompt
        /// </summary>
        public string ChatSystemPrompt { get; private set; }

        /// <summary>
        /// Token 选择偏好
        /// </summary>
        public float[] TokenSelectionBiases { get; private set; }

        /// <summary>
        /// 评估参数
        /// </summary>
        public int EvaluationScore { get; private set; }

        private PromptItemDto() { }
    }
}