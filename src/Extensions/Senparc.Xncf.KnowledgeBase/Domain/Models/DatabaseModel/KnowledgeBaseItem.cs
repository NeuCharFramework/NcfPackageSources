
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
    /// KnowledgeBasesDetail 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBaseItem))]//必须添加前缀，防止全系统中发生冲突
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
        /// 知识库Id
        /// </summary>
        public int KnowledgeBasesId { get; private set; }
        /// <summary>
        /// 内容类型
        /// </summary>
        public ContentType ContentType { get; private set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// 源文件名称
        /// </summary>
        [MaxLength(500)]
        public string FileName { get; private set; }

        /// <summary>
        /// 文本切片索引总数
        /// </summary>
        public int ChunkIndex { get; private set; }

        /// <summary>
        /// 是否已向量化
        /// </summary>
        public bool IsEmbedded { get; private set; }

        /// <summary>
        /// 向量化时间
        /// </summary>
        public DateTime? EmbeddedTime { get; private set; }


        /// <summary>
        /// 向量化成功后调用，更新状态和时间戳
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
