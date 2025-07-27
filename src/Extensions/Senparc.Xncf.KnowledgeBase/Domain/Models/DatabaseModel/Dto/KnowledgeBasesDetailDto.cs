
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto
{
    public class KnowledgeBasesDetailDto : DtoBase
    {
        public KnowledgeBasesDetailDto()
        {
        }

        public KnowledgeBasesDetailDto(string id,string knowledgeBasesId,int contentType,string content)
        {
            Id = id;
            KnowledgeBasesId = knowledgeBasesId;
            ContentType = contentType;
            Content = content;
        }

        public string Id { get; set; }
        /// <summary>
        /// 知识库Id
        /// </summary>
        [MaxLength(50)]
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
