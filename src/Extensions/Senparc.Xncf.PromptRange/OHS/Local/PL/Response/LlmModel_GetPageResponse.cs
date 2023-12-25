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

    public class LlmModel_GetPageItemResponse : BaseResponse
    {
        // public int Id { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Developer => "admin";

        public bool Show { get; set; }
    }
}