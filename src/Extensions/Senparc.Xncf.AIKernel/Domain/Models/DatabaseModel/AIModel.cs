/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIModel.cs
    File description: AIModel related implementation

    Created by: Senparc - 20231220

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260705
    Change description: v0.13.3-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.13.4-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.AI;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models;

namespace Senparc.Xncf.AIKernel.Models
{
    /// <summary>
    /// Stores provider, deployment, credential, token-limit, and visibility metadata
    /// for one AI model configuration.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AIModel))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class AIModel : EntityBase<int>
    {
        /// <summary>
        /// Gets the stable application alias used to select this model configuration.
        /// </summary>
        [Required, MaxLength(50)]
        public string Alias { get; private set; }


        /// <summary>
        /// Gets the provider model identifier sent to the AI platform.
        /// </summary>
        [Required, MaxLength(100)]
        public string ModelId { get; private set; }

        /// <summary>
        /// Gets the optional provider deployment or deployment-slot name.
        /// </summary>
        [MaxLength(150)]
        public string DeploymentName { get; private set; }

        /// <summary>
        /// Gets the provider endpoint used for model requests.
        /// </summary>
        [MaxLength(250)]
        public string Endpoint { get; private set; }

        /// <summary>
        /// Gets the AI platform adapter used to execute this model.
        /// </summary>
        [Required]
        public AiPlatform AiPlatform { get; private set; }

        /// <summary>
        /// Gets the capability category, such as chat, completion, or embedding.
        /// </summary>
        [Required]
        public ConfigModelType ConfigModelType { get; private set; }

        /// <summary>
        /// Gets the provider organization or tenant identifier, when required.
        /// </summary>
        [MaxLength(200)]
        public string OrganizationId { get; private set; }

        /// <summary>
        /// Gets the credential used to authenticate with the provider.
        /// </summary>
        [MaxLength(200)]
        public string ApiKey { get; private set; }

        /// <summary>
        /// Gets the provider API-version value, when the platform requires one.
        /// </summary>
        [MaxLength(100)]
        public string ApiVersion { get; private set; }

        /// <summary>
        /// Note (optional)
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        /// Gets the maximum token budget configured for this model.
        /// </summary>
        [Required, DefaultValue(0)]
        public int MaxToken { get; private set; }

        /// <summary>
        /// whether shared
        /// </summary>
        [Required, DefaultValue(false)]
        public bool IsShared { get; private set; } = false;


        /// <summary>
        /// whether displayed
        /// </summary>
        [Required, DefaultValue(true)]
        public bool Show { get; private set; }


        /// <summary>
        /// Initializes an AI model configuration with provider, endpoint, credential, and display settings.
        /// </summary>
        /// <param name="deploymentName">The provider deployment name.</param>
        /// <param name="endpoint">The provider endpoint.</param>
        /// <param name="aiPlatform">The AI provider platform.</param>
        /// <param name="organizationId">The optional organization identifier.</param>
        /// <param name="apiKey">The provider API key.</param>
        /// <param name="apiVersion">The optional provider API version.</param>
        /// <param name="note">The administrative note.</param>
        /// <param name="maxToken">The maximum token budget.</param>
        /// <param name="alias">The display alias.</param>
        /// <param name="modelId">The provider model identifier.</param>
        /// <param name="configModelType">The configured model capability type.</param>
        public AIModel(string deploymentName, string endpoint, AiPlatform aiPlatform, string organizationId, string apiKey,
            string apiVersion, string note, int maxToken, string alias, string modelId, ConfigModelType configModelType)
        {
            DeploymentName = deploymentName;
            Endpoint = endpoint;
            AiPlatform = aiPlatform;
            OrganizationId = organizationId;
            ApiKey = apiKey;
            ApiVersion = apiVersion;
            Note = note;
            MaxToken = maxToken;
            Alias = alias;
            Show = true;
            IsShared = false;
            ModelId = modelId;
            ConfigModelType = configModelType;
        }
        /// <summary>
        /// Initializes an AI model from an editable request DTO.
        /// </summary>
        /// <param name="orEditRequest">The model configuration request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="orEditRequest"/> is null.</exception>
        public AIModel(AIModel_CreateOrEditRequest orEditRequest) : this(orEditRequest.DeploymentName, orEditRequest.Endpoint,
            orEditRequest.AiPlatform, orEditRequest.OrganizationId, orEditRequest.ApiKey, orEditRequest.ApiVersion, orEditRequest.Note,
            orEditRequest.MaxToken, orEditRequest.Alias, orEditRequest.ModelId,orEditRequest.ConfigModelType)
        {
        }

        /// <summary>
        /// Changes whether this model is exposed in model-selection views.
        /// </summary>
        /// <param name="show">Whether the model should be visible.</param>
        /// <returns>This model instance after the visibility flag is updated.</returns>
        public AIModel SwitchShow(bool show)
        {
            Show = show;
            return this;
        }

        /// <summary>
        /// Applies editable model configuration values from a request.
        /// </summary>
        /// <param name="request">The replacement platform, model, credential, and visibility values.</param>
        /// <returns>This model instance after the values are applied.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
        public AIModel Update(AIModel_CreateOrEditRequest request)
        {
            ModelId = request.ModelId;
            ConfigModelType = request.ConfigModelType;
            DeploymentName = request.DeploymentName;
            Endpoint = request.Endpoint;
            AiPlatform = request.AiPlatform;
            OrganizationId = request.OrganizationId;
            ApiKey = request.ApiKey;
            ApiVersion = request.ApiVersion;
            Note = request.Note;
            MaxToken = request.MaxToken;
            Alias = request.Alias;

            IsShared = request.IsShared;
            SwitchShow(request.Show);
            return this;
        }
    }
}
