/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlModel_AddRequest.cs
    File description: LlModel_AddRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.AI;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class LlModel_AddRequest
    {
        /// <summary>
        /// code
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// Endpoint（must）
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
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