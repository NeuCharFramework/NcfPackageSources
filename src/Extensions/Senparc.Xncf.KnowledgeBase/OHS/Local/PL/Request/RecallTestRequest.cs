using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request
{
    public class RecallTestRequest
    {
        public int Id { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Number of recalled fragments Top K, default 5, range 1-20
        /// </summary>
        public int TopK { get; set; } = 5;
    }
}
