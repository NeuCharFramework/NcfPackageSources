/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseRequest.cs
    File description: KnowledgeBaseRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Request
{
    /// <summary>
    /// Carries editable knowledge-base configuration and source-file selections.
    /// </summary>
    public class KnowledgeBaseRequest
    {
        /// <summary>Gets or sets the knowledge-base identifier.</summary>
        public int Id { get; set; }
        /// <summary>Gets or sets the embedding model identifier.</summary>
        public int EmbeddingModelId { get; set; }
        /// <summary>Gets or sets the vector database configuration identifier.</summary>
        public int VectorDBId { get; set; }
        /// <summary>Gets or sets the chat model identifier used for retrieval answers.</summary>
        public int ChatModelId { get; set; }
        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        public string Content { get; set; }

        /// <summary>Gets or sets chunking and reranking configuration.</summary>
        public KnowledgeBaseRetrievalConfig RetrievalConfig { get; set; }

        /// <summary>Gets or sets managed FileManager file identifiers to attach.</summary>
        public List<int> NcfFileIds { get; set; }
    }
}
