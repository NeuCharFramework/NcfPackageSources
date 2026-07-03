/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptItem_GetRangeNameListResponse.cs
    文件功能描述：PromptItem_GetRangeNameListResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class PromptItem_GetRangeNameListResponse : AppResponseBase<string>
    {
        public int Id { get; set; }

        public string RangeName { get; set; }
    }
}