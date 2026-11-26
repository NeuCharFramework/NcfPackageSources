/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseEmbeddingStatus.cs
    File description: Related implementation.

    Modified by: Senparc - 20260815
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.KnowledgeBase.Domain.Services
{
    /// <summary>
    /// Identifies the lifecycle state of a knowledge-base chunk embedding.
    /// </summary>
    public enum KnowledgeBaseEmbeddingStatus
    {
        /// <summary>The chunk is awaiting embedding generation.</summary>
        Pending,
        /// <summary>The chunk uses a legacy or not-yet-migrated embedding representation.</summary>
        Legacy,
        /// <summary>The chunk has a current published embedding.</summary>
        Published
    }
}
