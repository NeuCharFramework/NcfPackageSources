/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_TextEmbeddingSearchRequest.cs
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
    public class PromptResult_TextEmbeddingSearchRequest
    {
        /// <summary>
        /// The prompt-result identifier whose TextEmbedding data should be searched.
        /// </summary>
        [Required]
        public int PromptResultId { get; set; }

        /// <summary>
        /// Gets or sets the semantic-search query text.
        /// </summary>
        [Required]
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of matching results to return.
        /// </summary>
        public int TopK { get; set; } = 3;
    }
}
