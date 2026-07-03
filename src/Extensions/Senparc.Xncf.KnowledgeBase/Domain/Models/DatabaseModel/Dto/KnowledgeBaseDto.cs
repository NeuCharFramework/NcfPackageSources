/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseDto.cs
    文件功能描述：KnowledgeBaseDto 相关实现
    
    
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
        /// 训练模型Id
        /// </summary>
        public int EmbeddingModelId { get; set; }
        /// <summary>
        /// 向量数据库Id
        /// </summary>
        public int VectorDBId { get; set; }
        /// <summary>
        /// 对话模型Id
        /// </summary>
        public int ChatModelId { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }

    }

    public class KnowledgeBase_InsertDto : KnowledgeBaseDto
    {
        public List<int> NcfFileIds { get; set; } = new List<int>();
    }
}
