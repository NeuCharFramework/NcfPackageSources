using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptItem_AddRequest
    {
        #region Model Config

        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; set; } = 0.5f;

        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature { get; set; } = 0.5f;

        /// <summary>
        /// 最大 Token 数
        /// </summary>
        [Required]
        public int MaxToken { get; set; } = 2000;

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public float FrequencyPenalty { get; set; }


        public float PresencePenalty { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        [Required] public int ModelId { get; set; }

        [Required] public string Content { get; set; }

        // public string Version { get; set; }


        /// <summary>
        /// 连发次数
        /// </summary>
        [Required]
        public int NumsOfResults { get; set; }

        [Required] public bool IsTopTactic { get; set; } = false;
        [Required] public bool IsNewTactic { get; set; } = false;
        [Required] public bool IsNewSubTactic { get; set; } = false;

        [Required] public bool IsNewAiming { get; set; } = false;

        public int? Id { get; set; }

        [Required] public int RangeId { get; set; }

        public string Note { get; set; }

        public string ExpectedResultsJson { get; set; }
        /// <summary>
        /// 是否启用“ai评分标准”
        /// </summary>
        public bool isAIGrade { get; set; } = false;

        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string VariableDictJson { get; set; }

        [Required] public bool IsDraft { get; set; }
        
        /// <summary>
        /// 对话模式下的用户消息（可选）
        /// </summary>
        public string UserMessage { get; set; }
        
        /// <summary>
        /// 继续聊天模式下的历史对话记录（可选）
        /// </summary>
        public List<ChatHistoryItem> ChatHistory { get; set; }
    }
    
    /// <summary>
    /// 对话历史记录项
    /// </summary>
    public class ChatHistoryItem
    {
        /// <summary>
        /// 角色：'user' 或 'assistant'
        /// </summary>
        public string Role { get; set; }
        
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }
    }
}