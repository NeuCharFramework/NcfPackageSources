/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: LlModel.cs

    File description: Related implementation.

    

    

    Created by: Senparc - 20231019

    

    Modified by: Senparc - 20260702

    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling



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
    ///Legacy comment translated to English.
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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required, MaxLength(20)]
        public AiPlatform ModelType { get; private set; }
        //Legacy comment translated to English.

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(200)]
        public string OrganizationId { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(200)]
        public string ApiKey { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(100)]
        public string ApiVersion { get; private set; }

        /// <summary>
        /// Note (optional)
        /// </summary>
        [MaxLength(1000)]
        public string Note { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
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

        public LlModel(LlModel_AddRequest request) : this(request.DeploymentName, request.Endpoint, request.ModelType,
            request.OrganizationId, request.ApiKey, request.ApiVersion, request.Note, 0, request.Alias, request.IsShared)
        {
        }

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


        public LlModel Switch(bool show)
        {
            Show = show;
            return this;
        }

        public LlModel Update(string alias, bool show, string deploymentName)
        {
            this.Alias = alias;
            this.DeploymentName = deploymentName;
            Switch(show);
            return this;
        }

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