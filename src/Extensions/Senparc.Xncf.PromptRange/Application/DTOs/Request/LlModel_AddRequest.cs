/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：LlModel_AddRequest.cs
    文件功能描述：LlModel_AddRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.AI;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class LlModel_AddRequest
    {
        /// <summary>
        /// 代号
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// Endpoint（必须）
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// 模型的类型（必须）, 例如：OpenAI,Azure OpenAI,HuggingFace
        /// </summary>
        public AiPlatform ModelType { get; set; }

        /// <summary>
        /// ApiKey
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// OrganizationId
        /// </summary>
        public string OrganizationId { get; set; }

        public string ApiVersion { get; set; }

        public string Note { get; set; } = "";

        public bool IsShared { get; set; } = false;
    }
}