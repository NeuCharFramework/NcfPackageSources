/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：LlmModel_ModifyRequest.cs
    文件功能描述：LlmModel_ModifyRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class LlmModel_ModifyRequest
    {
        public int Id { get; set; }
        public string Alias { get; set; }

        public string DeploymentName { get; set; }
        public bool Show { get; set; }
    }
}