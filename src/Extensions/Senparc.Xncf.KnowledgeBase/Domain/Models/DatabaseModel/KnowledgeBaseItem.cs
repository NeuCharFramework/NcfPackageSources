/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseItem.cs
    File description: KnowledgeBaseItem related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

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
    ///Legacy comment translated to English.
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
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int KnowledgeBasesId { get; private set; }
        /// <summary>
        /// contenttype
        /// </summary>
        public ContentType ContentType { get; private set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(500)]
        public string FileName { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int? NcfFileId { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int ChunkIndex { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsEmbedded { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime? EmbeddedTime { get; private set; }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void EmbeddingSucceeded()
        {
            this.IsEmbedded = true;
            this.EmbeddedTime = DateTime.Now;
        }

        public void ResetEmbedding()
        {
            IsEmbedded = false;
            EmbeddedTime = null;
        }


    }


    public enum ContentType
    {
        Text = 0,

        TextFile = 100,
        ImageFile = 200,
        AudioFile = 300,
        VideoFile = 400,
    }
}
