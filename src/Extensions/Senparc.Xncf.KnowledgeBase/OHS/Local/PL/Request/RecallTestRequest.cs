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
        /// 内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 召回片段数量 Top K，默认 5，范围 1-20
        /// </summary>
        public int TopK { get; set; } = 5;
    }
}
