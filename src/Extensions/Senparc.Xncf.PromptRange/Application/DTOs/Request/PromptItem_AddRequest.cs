/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem_AddRequest.cs
    File description: Related implementation.

    Created by: Senparc - 20231021

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260705

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

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
        /// maximum token count
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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public bool isAIGrade { get; set; } = false;

        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string VariableDictJson { get; set; }

        [Required] public bool IsDraft { get; set; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string UserMessage { get; set; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<ChatHistoryItem> ChatHistory { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string StreamId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public PromptExecutionOptions ExecutionOptions { get; set; }
    }
    
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class ChatHistoryItem
    {
        /// <summary>
        /// role：'user' or 'assistant'
        /// </summary>
        public string Role { get; set; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Content { get; set; }
    }
}
