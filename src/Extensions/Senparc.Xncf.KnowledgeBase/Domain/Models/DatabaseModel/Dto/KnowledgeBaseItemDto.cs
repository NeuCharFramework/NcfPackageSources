/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseItemDto.cs
    File description: KnowledgeBaseItemDto related implementation

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
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto
{
    public class KnowledgeBaseItemDto : DtoBase<int>
    {
        public KnowledgeBaseItemDto()
        {
        }

        public KnowledgeBaseItemDto(int id, int knowledgeBasesId, ContentType contentType, string content, string fileName = "", int chunkIndex = 0, int? ncfFileId = null)
        {
            Id = id;
            KnowledgeBasesId = knowledgeBasesId;
            ContentType = contentType;
            Content = content;
            FileName = fileName;
            ChunkIndex = chunkIndex;
            NcfFileId = ncfFileId;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int KnowledgeBasesId { get; set; }
        /// <summary>
        /// contenttype
        /// </summary>
        public ContentType ContentType { get; set; }

        public int? NcfFileId { get; set; }

        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(500)]
        public string FileName { get; set; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int ChunkIndex { get; set; }

    }
}
