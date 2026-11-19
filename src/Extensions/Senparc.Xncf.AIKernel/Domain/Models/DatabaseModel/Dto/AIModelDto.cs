/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIModelDto.cs
    File description: AIModelDto related implementation

    Created by: Senparc - 20231220

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using System;
using Senparc.AI;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto
{
    public class AIModelDto : DtoBase
    {
        /// <summary>
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// code
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// deployment name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// model name (required)
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// Endpoint（must）
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AiPlatform AiPlatform { get; set; }
        /// <summary>
        /// modeltype
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// whether shared
        /// </summary>
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// whether displayed
        /// </summary>
        public bool Show { get;  set; }
        
        /// <summary>
        /// Token usestatistics
        /// </summary>
        public AITokenModelUsage Usage { get; set; }

        public AIModelDto()
        {
        }

        public AIModelDto(AIModel aIModel)
        {
            Id = aIModel.Id;
            Alias = aIModel.Alias;
            ModelId = aIModel.ModelId;
            DeploymentName = aIModel.DeploymentName;
            Endpoint = aIModel.Endpoint;
            AiPlatform = aIModel.AiPlatform;
            ConfigModelType = aIModel.ConfigModelType;
            OrganizationId = aIModel.OrganizationId;
            ApiKey = aIModel.ApiKey;
            ApiVersion = aIModel.ApiVersion;
            Note = aIModel.Note;
            MaxToken = aIModel.MaxToken;
            IsShared = aIModel.IsShared;
            Show = aIModel.Show;
            
            AddTime = aIModel.AddTime;
            LastUpdateTime = aIModel.LastUpdateTime;
            TenantId = aIModel.TenantId;
        }
    }
}