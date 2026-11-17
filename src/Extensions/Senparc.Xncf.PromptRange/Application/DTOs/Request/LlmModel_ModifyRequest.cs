/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: LlmModel_ModifyRequest.cs
    File description: LlmModel_ModifyRequest related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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