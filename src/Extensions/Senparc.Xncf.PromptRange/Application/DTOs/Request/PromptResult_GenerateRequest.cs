/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_GenerateRequest.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_GenerateRequest
    {
        /// <summary>
        /// Gets or sets the prompt item identifier used for generation.
        /// </summary>
        [Required]
        public int PromptItemId { get; set; }

        /// <summary>
        /// Gets or sets the number of results requested from the generator.
        /// </summary>
        [Required]
        public int NumsOfResults { get; set; } = 1;

        /// <summary>
        /// Gets or sets the optional user message supplied to the prompt.
        /// </summary>
        public string UserMessage { get; set; }

        /// <summary>
        /// Gets or sets the optional stream identifier used for incremental output.
        /// </summary>
        public string StreamId { get; set; }

        /// <summary>
        /// Gets or sets execution-model and generation options.
        /// </summary>
        public PromptExecutionOptions ExecutionOptions { get; set; }
    }
}
