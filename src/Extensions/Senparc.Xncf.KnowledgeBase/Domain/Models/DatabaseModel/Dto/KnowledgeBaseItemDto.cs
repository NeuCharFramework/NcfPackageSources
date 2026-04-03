
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
        ///Knowledge BaseId
        /// </summary>
        public int KnowledgeBasesId { get; set; }
        /// <summary>
        /// content type
        /// </summary>
        public ContentType ContentType { get; set; }

        public int? NcfFileId { get; set; }

        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// source file name
        /// </summary>
        [MaxLength(500)]
        public string FileName { get; set; }
        
        /// <summary>
        ///text slice index
        /// </summary>
        public int ChunkIndex { get; set; }

    }
}
