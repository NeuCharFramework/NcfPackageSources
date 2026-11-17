/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_GenerateRequest.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_GenerateRequest
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int PromptItemId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int NumsOfResults { get; set; } = 1;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string UserMessage { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string StreamId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public PromptExecutionOptions ExecutionOptions { get; set; }
    }
}
