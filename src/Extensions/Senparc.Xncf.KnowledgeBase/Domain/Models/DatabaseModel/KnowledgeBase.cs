
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
    /// KnowledgeBase entity class
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBase))]//The prefix must be added to prevent conflicts system-wide.
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
        }
        /// <summary>
        ///Training model ID
        /// </summary>
        public int EmbeddingModelId { get; set; }
        /// <summary>
        ///Vector databaseId
        /// </summary>
        public int VectorDBId { get; set; }
        /// <summary>
        /// Dialog model ID
        /// </summary>
        public int ChatModelId { get; set; }
        /// <summary>
        ///name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
    }
}
