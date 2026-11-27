/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseDto.cs
    File description: KnowledgeBaseDto related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/


using Senparc.Ncf.Core.Models;
using Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto
{
    /// <summary>Transfers editable knowledge-base configuration to application services.</summary>
    public class KnowledgeBaseDto : DtoBase
    {
        public KnowledgeBaseDto()
        {
        }

        public KnowledgeBaseDto(int id, int embeddingModelId, int vectorDBId, int chatModelId, string name, string content)
        {
            Id = id;
            EmbeddingModelId = embeddingModelId;
            VectorDBId = vectorDBId;
            ChatModelId = chatModelId;
            Name = name;
            Content = content;
        }

        public int Id { get; set; }
        /// <summary>Gets or sets the embedding model identifier.</summary>
        public int EmbeddingModelId { get; set; }
        /// <summary>Gets or sets the vector database configuration identifier.</summary>
        public int VectorDBId { get; set; }
        /// <summary>Gets or sets the chat model identifier.</summary>
        public int ChatModelId { get; set; }
        /// <summary>
        /// name
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }

        public string VectorCollectionName { get; set; }

        public DateTime? EmbeddedTime { get; set; }

        /// <summary>Gets or sets the retrieval chunking and reranking configuration.</summary>
        public KnowledgeBaseRetrievalConfig RetrievalConfig { get; set; }

    }

    /// <summary>Extends knowledge-base configuration with managed source-file identifiers.</summary>
    public class KnowledgeBase_InsertDto : KnowledgeBaseDto
    {
        /// <summary>Gets or sets FileManager file identifiers to import.</summary>
        public List<int> NcfFileIds { get; set; }
    }
}
