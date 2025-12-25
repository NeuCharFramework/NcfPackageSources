
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto
{
    public class KnowledgeBasesDetailDto : DtoBase
    {
        public KnowledgeBasesDetailDto()
        {
        }

        public KnowledgeBasesDetailDto(string id,string knowledgeBasesId,int contentType,string content, string fileName = "", int chunkIndex = 0)
        {
            Id = id;
            KnowledgeBasesId = knowledgeBasesId;
            ContentType = contentType;
            Content = content;
            FileName = fileName;
            ChunkIndex = chunkIndex;
        }

        public string Id { get; set; }
        /// <summary>
        /// 知识库Id
        /// </summary>
        [MaxLength(50)]
        public string KnowledgeBasesId { get; set; }
        /// <summary>
        /// 内容类型
        /// </summary>
        public int ContentType { get; set; }
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
