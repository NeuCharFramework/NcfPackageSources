
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto
{
    public class KnowledgeBasesDto : DtoBase
    {
        public KnowledgeBasesDto()
        {
        }

        public KnowledgeBasesDto(string id,string embeddingModelId,string vectorDBId,string chatModelId,string name,string content)
        {
            Id = id;
            EmbeddingModelId = embeddingModelId;
            VectorDBId = vectorDBId;
            ChatModelId = chatModelId;
            Name = name;
            Content = content;
        }

        public string Id { get; set; }
        /// <summary>
        /// 训练模型Id
        /// </summary>
        [MaxLength(50)]
        public string EmbeddingModelId { get; set; }
        /// <summary>
        /// 向量数据库Id
        /// </summary>
        [MaxLength(50)]
        public string VectorDBId { get; set; }
        /// <summary>
        /// 对话模型Id
        /// </summary>
        [MaxLength(50)]
        public string ChatModelId { get; set; }
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
}
