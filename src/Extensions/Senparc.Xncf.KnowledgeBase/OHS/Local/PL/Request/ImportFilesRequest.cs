using System.Collections.Generic;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request
{
    public class ImportFilesRequest
    {
        public string knowledgeBaseId { get; set; }
        public List<int> fileIds { get; set; }
    }
}
