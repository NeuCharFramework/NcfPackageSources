/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlModel_GetPageResponse.cs
    File description: LlModel_GetPageResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

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
        /// user name
        /// </summary>
        public string Developer => "admin";

        public bool Show { get; set; }
    }
}