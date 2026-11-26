/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBase.cs
    File description: KnowledgeBase related implementation

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


using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel
{
    /// <summary>
    /// Represents a knowledge base, its source content, retrieval configuration,
    /// vector store binding, and embedding state.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBase))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class KnowledgeBase : EntityBase<int>
    {
        /// <summary>Initializes an empty knowledge base with audit timestamps.</summary>
        public KnowledgeBase()
        {
            AddTime = DateTime.Now;
            this.LastUpdateTime = AddTime;
        }
        /// <summary>Initializes a knowledge base from a DTO.</summary>
        /// <param name="knowledgeBasesDto">The knowledge-base values to copy.</param>
        public KnowledgeBase(KnowledgeBaseDto knowledgeBasesDto) : this()
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
            Content = knowledgeBasesDto.Content;
        }
        /// <summary>
        /// Replaces editable knowledge-base settings and invalidates existing embeddings.
        /// </summary>
        /// <param name="knowledgeBasesDto">The replacement knowledge-base values.</param>
        public void Update(KnowledgeBaseDto knowledgeBasesDto)
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
            Content = knowledgeBasesDto.Content;
            InvalidateEmbedding();
        }
        /// <summary>
        /// Gets or sets the embedding model identifier.
        /// </summary>
        public int EmbeddingModelId { get; set; }
        /// <summary>
        /// Gets or sets the vector database identifier.
        /// </summary>
        public int VectorDBId { get; set; }
        /// <summary>
        /// Gets or sets the chat model identifier.
        /// </summary>
        public int ChatModelId { get; set; }
        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the serialized retrieval configuration.
        /// </summary>
        [MaxLength(2000)]
        public string RetrievalConfigJson { get; set; }

        /// <summary>
        /// Parses the persisted retrieval configuration or returns its defaults.
        /// </summary>
        public KnowledgeBaseRetrievalConfig GetRetrievalConfig()
        {
            return KnowledgeBaseRetrievalConfig.FromJson(RetrievalConfigJson);
        }

        /// <summary>
        /// Stores a sanitized retrieval configuration, omitting default settings.
        /// </summary>
        public void SetRetrievalConfig(KnowledgeBaseRetrievalConfig config)
        {
            RetrievalConfigJson = config == null || config.IsDefault()
                ? null
                : config.Clone().Sanitized().ToJson();
        }

        /// <summary>
        /// Gets the vector collection created by the last successful embedding.
        /// </summary>
        [MaxLength(200)]
        public string VectorCollectionName { get; private set; }

        /// <summary>
        /// Gets the time of the last successful embedding.
        /// </summary>
        public DateTime? EmbeddedTime { get; private set; }

        /// <summary>
        /// Marks embedding as complete and stores the target vector collection name.
        /// </summary>
        /// <param name="vectorCollectionName">The vector collection identifier.</param>
        /// <exception cref="ArgumentException">Thrown when the collection name is blank.</exception>
        public void MarkEmbeddingCompleted(string vectorCollectionName)
        {
            if (string.IsNullOrWhiteSpace(vectorCollectionName))
            {
                throw new ArgumentException("向量集合名称不能为空。", nameof(vectorCollectionName));
            }

            VectorCollectionName = vectorCollectionName;
            EmbeddedTime = DateTime.Now;
        }

        /// <summary>Clears vector collection and embedding timestamp state.</summary>
        public void InvalidateEmbedding()
        {
            VectorCollectionName = null;
            EmbeddedTime = null;
        }

        /// <summary>Replaces inline content and invalidates existing embeddings.</summary>
        /// <param name="content">The new inline content.</param>
        public void SetContent(string content)
        {
            Content = content;
            InvalidateEmbedding();
        }
    }
}
