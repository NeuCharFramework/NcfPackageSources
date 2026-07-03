/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：LlModel_GetPageResponse.cs
    文件功能描述：LlModel_GetPageResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        /// 模型名称
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Developer => "admin";

        public bool Show { get; set; }
    }
}