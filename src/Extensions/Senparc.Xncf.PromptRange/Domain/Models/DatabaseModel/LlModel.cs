/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlModel.cs
    File description: Related implementation.

    Created by: Senparc - 20231019

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.AI;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    /// <summary>
    /// Stores one PromptRange language-model provider configuration.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(LlModel))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class LlModel : EntityBase<int>
    {
        /// <summary>
        /// code
        /// </summary>
        [Required, MaxLength(50)]
        public string Alias { get; private set; }

        /// <summary>
        /// name（must）
        /// </summary>
        [Required, MaxLength(100)]
        public string DeploymentName { get; private set; }

        /// <summary>
        /// Endpoint（must）
        /// </summary>
        [Required, MaxLength(250)]
        public string Endpoint { get; private set; }

        /// <summary>Gets the AI provider platform.</summary>
        [Required, MaxLength(20)]
        public AiPlatform ModelType { get; private set; }
        /// <summary>Gets the provider organization identifier.</summary>
        [MaxLength(200)]
        public string OrganizationId { get; private set; }

        /// <summary>Gets the provider API key.</summary>
        [MaxLength(200)]
        public string ApiKey { get; private set; }

        /// <summary>Gets the provider API version.</summary>
        [MaxLength(100)]
        public string ApiVersion { get; private set; }

        /// <summary>
        /// Note (optional)
        /// </summary>
        [MaxLength(1000)]
        public string Note { get; private set; }

        /// <summary>Gets the maximum token limit.</summary>
        [Required, DefaultValue(0)]
        public int MaxToken { get; private set; }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // [MaxLength(100)]
        // public string TextCompletionModelName { get; private set; }
        //
        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // [MaxLength(100)]
        // public string TextEmbeddingModelName { get; private set; }
        //
        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // [MaxLength(100)]
        // public string OtherModelName { get; private set; }

        /// <summary>
        /// whether shared
        /// </summary>
        public bool IsShared { get; private set; }

        /// <summary>
        /// whether displayed
        /// </summary>
        public bool Show { get; private set; }


        #region CTOR

        /// <summary>Initializes an LLM model from a textual AI platform value.</summary>
        /// <param name="deploymentName">The deployment/model name.</param>
        /// <param name="endpoint">The provider endpoint.</param>
        /// <param name="modelType">The AI platform name.</param>
        /// <param name="organizationId">The optional organization identifier.</param>
        /// <param name="apiKey">The provider API key.</param>
        /// <param name="apiVersion">The provider API version.</param>
        /// <param name="note">The model note.</param>
        /// <param name="maxToken">The maximum token setting.</param>
        /// <param name="alias">The model alias.</param>
        /// <param name="show">Whether the model is displayed.</param>
        /// <exception cref="NcfExceptionBase">Thrown when <paramref name="modelType"/> is unsupported.</exception>
        public LlModel(string deploymentName, string endpoint, string modelType, string organizationId, string apiKey,
            string apiVersion, string note, int maxToken, string alias, bool show)
        {
            DeploymentName = deploymentName;
            Endpoint = endpoint;
            AiPlatform platform = Enum.TryParse<AiPlatform>(modelType, ignoreCase: true, out var aiPlatform)
                ? aiPlatform
                : throw new NcfExceptionBase($"没有该模型类型：{modelType}");
            ModelType = platform;
            OrganizationId = organizationId;
            ApiKey = apiKey;
            ApiVersion = apiVersion;
            Note = note;
            MaxToken = maxToken;
            Alias = alias;
            Show = show;
        }

        /// <summary>Initializes an LLM model from a typed AI platform value.</summary>
        /// <param name="deploymentName">The deployment/model name.</param>
        /// <param name="endpoint">The provider endpoint.</param>
        /// <param name="modelType">The AI platform.</param>
        /// <param name="organizationId">The optional organization identifier.</param>
        /// <param name="apiKey">The provider API key.</param>
        /// <param name="apiVersion">The provider API version.</param>
        /// <param name="note">The model note.</param>
        /// <param name="maxToken">The maximum token setting.</param>
        /// <param name="alias">The model alias.</param>
        /// <param name="show">Whether the model is displayed.</param>
        public LlModel(string deploymentName, string endpoint, AiPlatform modelType, string organizationId, string apiKey,
            string apiVersion, string note, int maxToken, string alias, bool show)
        {
            DeploymentName = deploymentName;
            Endpoint = endpoint;
            ModelType = modelType;
            OrganizationId = organizationId;
            ApiKey = apiKey;
            ApiVersion = apiVersion;
            Note = note;
            MaxToken = maxToken;
            Alias = alias;
            Show = show;
        }

        /// <summary>Initializes an LLM model from an add request.</summary>
        /// <param name="request">The model add request.</param>
        public LlModel(LlModel_AddRequest request) : this(request.DeploymentName, request.Endpoint, request.ModelType,
            request.OrganizationId, request.ApiKey, request.ApiVersion, request.Note, 0, request.Alias, request.IsShared)
        {
        }

        /// <summary>Initializes an LLM model from a DTO.</summary>
        /// <param name="llModelDto">The source DTO.</param>
        public LlModel(LlModelDto llModelDto)
        {
            Show = llModelDto.IsShared;
            Alias = llModelDto.Alias;
            DeploymentName = llModelDto.DeploymentName;
            Endpoint = llModelDto.Endpoint;
            OrganizationId = llModelDto.OrganizationId;
            ApiKey = llModelDto.ApiKey;
            ApiVersion = llModelDto.ApiVersion;
            Note = llModelDto.Note;
            MaxToken = llModelDto.MaxToken;
            // TextCompletionModelName = llmModelDto.TextCompletionModelName;
            // TextEmbeddingModelName = llmModelDto.TextEmbeddingModelName;
            // OtherModelName = llmModelDto.OtherModelName;
        }

        #endregion


        /// <summary>Sets whether the model is displayed.</summary>
        /// <param name="show">The desired display state.</param>
        /// <returns>This model entity.</returns>
        public LlModel Switch(bool show)
        {
            Show = show;
            return this;
        }

        /// <summary>Updates the alias, display state, and deployment name.</summary>
        /// <param name="alias">The model alias.</param>
        /// <param name="show">The display state.</param>
        /// <param name="deploymentName">The deployment/model name.</param>
        /// <returns>This model entity.</returns>
        public LlModel Update(string alias, bool show, string deploymentName)
        {
            this.Alias = alias;
            this.DeploymentName = deploymentName;
            Switch(show);
            return this;
        }

        /// <summary>Returns the effective model identifier used for provider calls.</summary>
        /// <returns>The deployment-derived model identifier or the default text model.</returns>
        public string GetModelId()
        {
            if (string.IsNullOrWhiteSpace(this.DeploymentName))
            {
                return "text-davinci-003";
            }

            return this.DeploymentName.Contains("azure") ? this.DeploymentName.Substring("azure-".Length) : this.DeploymentName;
        }
    }
}
