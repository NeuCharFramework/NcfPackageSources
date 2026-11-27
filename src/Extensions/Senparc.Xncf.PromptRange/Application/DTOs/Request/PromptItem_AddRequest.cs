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

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

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


        /// <summary>Gets or sets the presence penalty applied during generation.</summary>
        public float PresencePenalty { get; set; }

        /// <summary>
        /// Gets or sets optional stop sequences passed to the model.
        /// </summary>
        public string StopSequences { get; set; }

        #endregion

        /// <summary>Gets or sets the AI model identifier.</summary>
        [Required] public int ModelId { get; set; }

        /// <summary>Gets or sets the prompt content.</summary>
        [Required] public string Content { get; set; }

        // public string Version { get; set; }


        /// <summary>
        /// Gets or sets the number of generated result variants.
        /// </summary>
        [Required]
        public int NumsOfResults { get; set; }

        /// <summary>Gets or sets whether this item is a top-level tactic.</summary>
        [Required] public bool IsTopTactic { get; set; } = false;
        /// <summary>Gets or sets whether a new tactic should be created.</summary>
        [Required] public bool IsNewTactic { get; set; } = false;
        /// <summary>Gets or sets whether a new sub-tactic should be created.</summary>
        [Required] public bool IsNewSubTactic { get; set; } = false;

        /// <summary>Gets or sets whether a new aiming branch should be created.</summary>
        [Required] public bool IsNewAiming { get; set; } = false;

        /// <summary>Gets or sets the existing prompt-item identifier for an edit request.</summary>
        public int? Id { get; set; }

        /// Gets or sets the owning PromptRange identifier.
        [Required] public int RangeId { get; set; }

        /// <summary>Gets or sets an administrative note.</summary>
        public string Note { get; set; }

        /// <summary>Gets or sets serialized expected results used by evaluation.</summary>
        public string ExpectedResultsJson { get; set; }
        /// <summary>Gets or sets whether automatic grading is enabled.</summary>
        public bool isAIGrade { get; set; } = false;

        /// <summary>Gets or sets the variable prefix used during prompt rendering.</summary>
        public string Prefix { get; set; }
        /// <summary>Gets or sets the variable suffix used during prompt rendering.</summary>
        public string Suffix { get; set; }
        /// <summary>Gets or sets serialized variable definitions.</summary>
        public string VariableDictJson { get; set; }

        /// <summary>Gets or sets whether the prompt item is a draft.</summary>
        [Required] public bool IsDraft { get; set; }
        
        /// <summary>Gets or sets the user message used for chat continuation.</summary>
        public string UserMessage { get; set; }
        
        /// <summary>Gets or sets prior chat messages supplied to the execution.</summary>
        public List<ChatHistoryItem> ChatHistory { get; set; }

        /// <summary>Gets or sets the stream identifier used for incremental output.</summary>
        public string StreamId { get; set; }

        /// <summary>Gets or sets advanced execution options.</summary>
        public PromptExecutionOptions ExecutionOptions { get; set; }
    }
    
    /// <summary>Represents one prior user or assistant message in a chat request.</summary>
    public class ChatHistoryItem
    {
        /// <summary>
        /// role：'user' or 'assistant'
        /// </summary>
        public string Role { get; set; }
        
        /// <summary>Gets or sets the message content.</summary>
        public string Content { get; set; }
    }
}
