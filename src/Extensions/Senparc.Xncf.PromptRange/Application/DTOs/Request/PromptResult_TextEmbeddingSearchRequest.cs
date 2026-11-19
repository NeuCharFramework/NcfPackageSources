/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_TextEmbeddingSearchRequest.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_TextEmbeddingSearchRequest
    {
        /// <summary>
        /// PromptResult ID（mustfor TextEmbedding typeresult）
        /// </summary>
        [Required]
        public int PromptResultId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string Query { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int TopK { get; set; } = 3;
    }
}
