
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
    /// KnowledgeBasesDetail entity class
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBaseItem))]//The prefix must be added to prevent conflicts system-wide.
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
        }
        public void Update(KnowledgeBaseItemDto knowledgeBaseItemDto)
        {
            KnowledgeBasesId = knowledgeBaseItemDto.KnowledgeBasesId;
            ContentType = knowledgeBaseItemDto.ContentType;
            Content = knowledgeBaseItemDto.Content;
            FileName = knowledgeBaseItemDto.FileName;
            ChunkIndex = knowledgeBaseItemDto.ChunkIndex;
        }
        /// <summary>
        ///Knowledge BaseId
        /// </summary>
        public int KnowledgeBasesId { get; private set; }
        /// <summary>
        /// content type
        /// </summary>
        public ContentType ContentType { get; private set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// source file name
        /// </summary>
        [MaxLength(500)]
        public string FileName { get; private set; }

        /// <summary>
        ///Total number of text slice indexes
        /// </summary>
        public int ChunkIndex { get; private set; }

        /// <summary>
        /// Whether it has been vectorized
        /// </summary>
        public bool IsEmbedded { get; private set; }

        /// <summary>
        ///vectorization time
        /// </summary>
        public DateTime? EmbeddedTime { get; private set; }


        /// <summary>
        /// Called after successful vectorization, update status and timestamp
        /// </summary>
        public void EmbeddingSuccessed(int totalChunkIndex)
        {
            this.ChunkIndex = totalChunkIndex;
            this.IsEmbedded = true;
            this.EmbeddedTime = DateTime.Now;
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
