
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
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBase))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class KnowledgeBase : EntityBase<int>
    {
        public KnowledgeBase()
        {
            AddTime = DateTime.Now;
            this.LastUpdateTime = AddTime;
        }
        public KnowledgeBase(KnowledgeBasesDto knowledgeBasesDto) : this()
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
            Content = knowledgeBasesDto.Content;
        }
        public void Update(KnowledgeBasesDto knowledgeBasesDto)
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
            Content = knowledgeBasesDto.Content;
        }
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
        public string Name { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
    }
}
