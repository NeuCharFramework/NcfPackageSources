using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request
{
    public class KnowledgeBasesDetailRequest
    {
        /// <summary>
        ///Knowledge BaseId
        /// </summary>
        public string KnowledgeBasesId { get; set; }
        /// <summary>
        /// content type
        /// </summary>
        public int ContentType { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
    }
}
