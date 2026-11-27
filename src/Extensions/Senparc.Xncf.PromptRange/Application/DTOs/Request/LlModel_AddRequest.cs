/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlModel_AddRequest.cs
    File description: LlModel_AddRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.AI;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>Contains provider and credential values for adding a PromptRange language model.</summary>
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

        /// <summary>Gets or sets the AI provider platform.</summary>
        public AiPlatform ModelType { get; set; }

        /// <summary>
        /// ApiKey
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// OrganizationId
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>Gets or sets the provider API version.</summary>
        public string ApiVersion { get; set; }

        /// <summary>Gets or sets an optional administrative note.</summary>
        public string Note { get; set; } = "";

        /// <summary>Gets or sets whether the model is shared.</summary>
        public bool IsShared { get; set; } = false;
    }
}
