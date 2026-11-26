/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_ContinueChatRequest.cs
    File description: Related implementation.

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>Requests continuation of an existing prompt-result chat.</summary>
    public class PromptResult_ContinueChatRequest
    {
        /// <summary>Gets or sets the prompt-result chat identifier.</summary>
        [Required]
        public int PromptResultId { get; set; }

        /// <summary>Gets or sets the new user message.</summary>
        [Required]
        public string UserMessage { get; set; }

        /// <summary>Gets or sets the optional stream identifier.</summary>
        public string StreamId { get; set; }
    }
}
