using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Request
{
    public class KnowledgeBaseItemRequest
    {
        /// <summary>
        ///Knowledge BaseId
        /// </summary>
        public int KnowledgeBasesId { get; set; }
        /// <summary>
        /// content type
        /// </summary>
        public ContentType ContentType { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
    }
}
