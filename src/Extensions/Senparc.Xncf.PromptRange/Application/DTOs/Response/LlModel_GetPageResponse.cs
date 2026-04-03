using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class LlModel_GetPageResponse
    {
        public LlModel_GetPageResponse(IEnumerable<LlmModel_GetPageItemResponse> list, int TotalCount)
        {
            List = list;
            this.TotalCount = TotalCount;
        }

        public IEnumerable<LlmModel_GetPageItemResponse> List { get; }

        public int TotalCount { get; }
    }

    public class LlmModel_GetPageItemResponse : BaseResponse
    {
        // public int Id { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// username
        /// </summary>
        public string Developer => "admin";

        public bool Show { get; set; }
    }
}