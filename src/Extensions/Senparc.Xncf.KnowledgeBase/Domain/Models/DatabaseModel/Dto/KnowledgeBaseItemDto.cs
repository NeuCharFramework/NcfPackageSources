/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseItemDto.cs
    文件功能描述：KnowledgeBaseItemDto 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/


using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto
{
    public class KnowledgeBaseItemDto : DtoBase<int>
    {
        public KnowledgeBaseItemDto()
        {
        }

        public KnowledgeBaseItemDto(int id, int knowledgeBasesId, ContentType contentType, string content, string fileName = "", int chunkIndex = 0)
        {
            Id = id;
            KnowledgeBasesId = knowledgeBasesId;
            ContentType = contentType;
            Content = content;
            FileName = fileName;
            ChunkIndex = chunkIndex;
        }

        /// <summary>
        /// 知识库Id
        /// </summary>
        public int KnowledgeBasesId { get; set; }
        /// <summary>
        /// 内容类型
        /// </summary>
        public ContentType ContentType { get; set; }

        public int? NcfFileId { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// 源文件名称
        /// </summary>
        [MaxLength(500)]
        public string FileName { get; set; }
        
        /// <summary>
        /// 文本切片索引
        /// </summary>
        public int ChunkIndex { get; set; }

    }
}
