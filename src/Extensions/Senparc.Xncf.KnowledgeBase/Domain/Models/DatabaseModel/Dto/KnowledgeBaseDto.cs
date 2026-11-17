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
        [MaxLength(100)]
        public string Name { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }

        public string VectorCollectionName { get; set; }

        public DateTime? EmbeddedTime { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public KnowledgeBaseRetrievalConfig RetrievalConfig { get; set; }

    }

    public class KnowledgeBase_InsertDto : KnowledgeBaseDto
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<int> NcfFileIds { get; set; }
    }
}
