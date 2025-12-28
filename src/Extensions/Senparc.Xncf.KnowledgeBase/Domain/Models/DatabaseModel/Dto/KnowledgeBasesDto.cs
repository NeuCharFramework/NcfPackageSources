
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

        public KnowledgeBasesDto(int id, int embeddingModelId, int vectorDBId, int chatModelId, string name, string content)
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
}
