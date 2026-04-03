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
        /// temperature
        /// </summary>
        public float Temperature { get; set; } = 0.5f;

        /// <summary>
        ///Maximum number of Tokens
        /// </summary>
        [Required]
        public int MaxToken { get; set; } = 2000;

        /// <summary>
        /// frequency penalty
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
        ///number of bursts
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
        /// Whether to enable "ai scoring criteria"
        /// </summary>
        public bool isAIGrade { get; set; } = false;

        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string VariableDictJson { get; set; }

        [Required] public bool IsDraft { get; set; }
        
        /// <summary>
        /// User messages in conversation mode (optional)
        /// </summary>
        public string UserMessage { get; set; }
        
        /// <summary>
        /// Continue historical conversation in chat mode (optional)
        /// </summary>
        public List<ChatHistoryItem> ChatHistory { get; set; }
    }
    
    /// <summary>
    ///conversation history items
    /// </summary>
    public class ChatHistoryItem
    {
        /// <summary>
        /// Role: 'user' or 'assistant'
        /// </summary>
        public string Role { get; set; }
        
        /// <summary>
        /// message content
        /// </summary>
        public string Content { get; set; }
    }
}