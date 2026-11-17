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
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBase))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class KnowledgeBase : EntityBase<int>
    {
        public KnowledgeBase()
        {
            AddTime = DateTime.Now;
            this.LastUpdateTime = AddTime;
        }
        public KnowledgeBase(KnowledgeBaseDto knowledgeBasesDto) : this()
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
            Content = knowledgeBasesDto.Content;
        }
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
        ///Legacy comment translated to English.
        /// </summary>
        public int EmbeddingModelId { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int VectorDBId { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(2000)]
        public string RetrievalConfigJson { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public KnowledgeBaseRetrievalConfig GetRetrievalConfig()
        {
            return KnowledgeBaseRetrievalConfig.FromJson(RetrievalConfigJson);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void SetRetrievalConfig(KnowledgeBaseRetrievalConfig config)
        {
            RetrievalConfigJson = config == null || config.IsDefault()
                ? null
                : config.Clone().Sanitized().ToJson();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(200)]
        public string VectorCollectionName { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime? EmbeddedTime { get; private set; }

        public void MarkEmbeddingCompleted(string vectorCollectionName)
        {
            if (string.IsNullOrWhiteSpace(vectorCollectionName))
            {
                throw new ArgumentException("向量集合名称不能为空。", nameof(vectorCollectionName));
            }

            VectorCollectionName = vectorCollectionName;
            EmbeddedTime = DateTime.Now;
        }

        public void InvalidateEmbedding()
        {
            VectorCollectionName = null;
            EmbeddedTime = null;
        }

        public void SetContent(string content)
        {
            Content = content;
            InvalidateEmbedding();
        }
    }
}
