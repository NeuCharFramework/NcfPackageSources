/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBase.cs
    文件功能描述：KnowledgeBase 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview6 新增知识库生命周期管理与 Agent 模板集成

----------------------------------------------------------------*/


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
    /// KnowledgeBase 实体类
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
        public KnowledgeBase(KnowledgeBaseDto knowledgeBasesDto) : this()
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
            Content = knowledgeBasesDto.Content;
        }
        public void Update(KnowledgeBaseDto knowledgeBasesDto)
        {
            EmbeddingModelId = knowledgeBasesDto.EmbeddingModelId;
            VectorDBId = knowledgeBasesDto.VectorDBId;
            ChatModelId = knowledgeBasesDto.ChatModelId;
            Name = knowledgeBasesDto.Name;
            Content = knowledgeBasesDto.Content;
            InvalidateEmbedding();
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

        /// <summary>
        /// 当前已发布的向量集合名称。仅在整批向量化成功后更新。
        /// </summary>
        [MaxLength(200)]
        public string VectorCollectionName { get; private set; }

        /// <summary>
        /// 最近一次完整向量化成功时间。
        /// </summary>
        public DateTime? EmbeddedTime { get; private set; }

        public void MarkEmbeddingCompleted(string vectorCollectionName)
        {
            if (string.IsNullOrWhiteSpace(vectorCollectionName))
            {
                throw new ArgumentException("向量集合名称不能为空。", nameof(vectorCollectionName));
            }

            VectorCollectionName = vectorCollectionName;
            EmbeddedTime = DateTime.Now;
        }

        public void InvalidateEmbedding()
        {
            VectorCollectionName = null;
            EmbeddedTime = null;
        }

        public void SetContent(string content)
        {
            Content = content;
            InvalidateEmbedding();
        }
    }
}
