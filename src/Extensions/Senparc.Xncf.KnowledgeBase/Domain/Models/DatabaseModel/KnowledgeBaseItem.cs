/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseItem.cs
    File description: KnowledgeBaseItem related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/


using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel
{
    /// <summary>
    /// Stores one chunk of source content and its embedding-processing state.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBaseItem))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class KnowledgeBaseItem : EntityBase<int>
    {
        public KnowledgeBaseItem()
        {
            AddTime = DateTime.Now;
            this.LastUpdateTime = AddTime;
            AdminRemark = string.Empty;
            Remark = string.Empty;
        }
        public KnowledgeBaseItem(KnowledgeBaseItemDto knowledgeBaseItemDto) : this()
        {
            KnowledgeBasesId = knowledgeBaseItemDto.KnowledgeBasesId;
            ContentType = knowledgeBaseItemDto.ContentType;
            Content = knowledgeBaseItemDto.Content;
            FileName = knowledgeBaseItemDto.FileName;
            ChunkIndex = knowledgeBaseItemDto.ChunkIndex;
            NcfFileId = knowledgeBaseItemDto.NcfFileId;
        }
        /// <summary>
        /// Replaces the persisted chunk metadata and content from a DTO, then clears the
        /// embedding state because the source text has changed.
        /// </summary>
        /// <param name="knowledgeBaseItemDto">The DTO containing the replacement chunk values.</param>
        public void Update(KnowledgeBaseItemDto knowledgeBaseItemDto)
        {
            KnowledgeBasesId = knowledgeBaseItemDto.KnowledgeBasesId;
            ContentType = knowledgeBaseItemDto.ContentType;
            Content = knowledgeBaseItemDto.Content;
            FileName = knowledgeBaseItemDto.FileName;
            ChunkIndex = knowledgeBaseItemDto.ChunkIndex;
            NcfFileId = knowledgeBaseItemDto.NcfFileId;
            ResetEmbedding();
        }
        /// <summary>Gets the owning knowledge-base identifier.</summary>
        public int KnowledgeBasesId { get; private set; }
        /// <summary>
        /// contenttype
        /// </summary>
        public ContentType ContentType { get; private set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; private set; }

        /// <summary>Gets the original source file name, when the chunk came from a file.</summary>
        [MaxLength(500)]
        public string FileName { get; private set; }

        /// <summary>Gets the optional managed FileManager file identifier.</summary>
        public int? NcfFileId { get; private set; }

        /// <summary>Gets the zero-based chunk index within the source content.</summary>
        public int ChunkIndex { get; private set; }

        /// <summary>Gets a value indicating whether an embedding has been generated.</summary>
        public bool IsEmbedded { get; private set; }

        /// <summary>Gets the local timestamp of the last successful embedding.</summary>
        public DateTime? EmbeddedTime { get; private set; }


        /// <summary>
        /// Marks the chunk as successfully embedded and records the embedding timestamp.
        /// </summary>
        public void EmbeddingSucceeded()
        {
            this.IsEmbedded = true;
            this.EmbeddedTime = DateTime.Now;
        }

        /// <summary>
        /// Clears the embedding flag and timestamp so the chunk can be processed again.
        /// </summary>
        public void ResetEmbedding()
        {
            IsEmbedded = false;
            EmbeddedTime = null;
        }


    }


    /// <summary>Identifies the source modality represented by a knowledge-base item.</summary>
    public enum ContentType
    {
        Text = 0,

        TextFile = 100,
        ImageFile = 200,
        AudioFile = 300,
        VideoFile = 400,
    }
}
