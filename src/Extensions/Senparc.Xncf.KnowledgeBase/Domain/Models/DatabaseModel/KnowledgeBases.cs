
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
    /// KnowledgeBases 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBases))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class KnowledgeBases : EntityBase<string>
    {
        public KnowledgeBases()
        {
            Id = Guid.NewGuid().ToString();
            AddTime = DateTime.Now;
            this.LastUpdateTime = AddTime;
        }
        public KnowledgeBases(KnowledgeBasesDto knowledgeBasesDto) : this()
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
        }
        public void Update(KnowledgeBasesDto knowledgeBasesDto)
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
        }
        /// <summary>
        /// 训练模型Id
        /// </summary>
        public string EmbeddingModelId { get; set; }
        /// <summary>
        /// 向量数据库Id
        /// </summary>
        public string VectorDBId { get; set; }
        /// <summary>
        /// 对话模型Id
        /// </summary>
        public string ChatModelId { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

    }
}
