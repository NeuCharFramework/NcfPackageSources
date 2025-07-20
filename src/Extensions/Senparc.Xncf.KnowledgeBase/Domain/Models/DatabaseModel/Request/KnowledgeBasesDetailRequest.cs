using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Request
{
    public class KnowledgeBasesDetailRequest
    {
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
