/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_ContinueChatRequest.cs
    File description: Related implementation.

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class PromptResult_ContinueChatRequest
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int PromptResultId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string UserMessage { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string StreamId { get; set; }
    }
}
