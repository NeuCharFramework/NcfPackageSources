using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class LlmModel_GetPageResponse
    {
        public LlmModel_GetPageResponse(IEnumerable<LlmModel_GetPageItemResponse> list, int TotalCount)
        {
            List = list;
            this.TotalCount = TotalCount;
        }
        public IEnumerable<LlmModel_GetPageItemResponse> List { get; }

        public int TotalCount { get; }
    }
}
