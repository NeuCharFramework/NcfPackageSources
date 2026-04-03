using Senparc.AI;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class LlModel_AddRequest
    {
        /// <summary>
        /// code name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        ///name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        ///Endpoint (required)
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Model type (required), for example: OpenAI, Azure OpenAI, HuggingFace
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