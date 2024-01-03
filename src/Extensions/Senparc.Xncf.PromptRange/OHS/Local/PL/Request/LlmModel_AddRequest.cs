using Senparc.AI;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class LlmModel_AddRequest
    {
        /// <summary>
        /// 代号
        /// </summary>
        public string Alias { get;  set; }

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

        public string Note { get; set; }

        public bool IsShared { get; set; }
    }
}