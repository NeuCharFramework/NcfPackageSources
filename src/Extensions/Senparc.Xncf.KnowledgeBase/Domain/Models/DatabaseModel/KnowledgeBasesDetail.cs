
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
    /// KnowledgeBasesDetail 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(KnowledgeBasesDetail))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class KnowledgeBasesDetail : EntityBase<string>
    {
        public KnowledgeBasesDetail()
        {
            Id = Guid.NewGuid().ToString();
            AddTime = DateTime.Now;
            this.LastUpdateTime = AddTime;
        }
        public KnowledgeBasesDetail(KnowledgeBasesDetailDto knowledgeBasesDetailDto) : this()
        {
            KnowledgeBasesId = knowledgeBasesDetailDto.KnowledgeBasesId;
            ContentType = knowledgeBasesDetailDto.ContentType;
            Content = knowledgeBasesDetailDto.Content;
        }
        public void Update(KnowledgeBasesDetailDto knowledgeBasesDetailDto)
        {
            KnowledgeBasesId = knowledgeBasesDetailDto.KnowledgeBasesId;
            ContentType = knowledgeBasesDetailDto.ContentType;
            Content = knowledgeBasesDetailDto.Content;
        }
        /// <summary>
        /// 知识库Id
        /// </summary>
        public string KnowledgeBasesId { get; set; }
        /// <summary>
        /// 内容类型
        /// </summary>
        public int ContentType { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }

    }
}
