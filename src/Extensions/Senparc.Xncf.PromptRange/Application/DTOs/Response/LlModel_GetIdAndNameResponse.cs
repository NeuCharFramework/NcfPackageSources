/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：LlModel_GetIdAndNameResponse.cs
    文件功能描述：LlModel_GetIdAndNameResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class LlModel_GetIdAndNameResponse : BaseResponse
    {
        // public int Id { get; set; }

        public string Alias { get; set; }
    }
}