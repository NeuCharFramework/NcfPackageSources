using System;
using Senparc.AI;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto
{
    public class AIModelDto : DtoBase
    {
        /// <summary>
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// code name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        ///deployment name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        ///Model name (required)
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        ///Endpoint (required)
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Type of model (required), for example: NeuCharAI, OpenAI, Azure OpenAI, HuggingFace
        /// </summary>
        public AiPlatform AiPlatform { get; set; }
        /// <summary>
        /// model type
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }


        /// <summary>
        ///OrganizationId (optional)
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        ///ApiKey (optional)
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        ///ApiVersion (optional)
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        ///Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        ///MaxToken (optional)
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// Whether to share
        /// </summary>
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// Whether to display
        /// </summary>
        public bool Show { get;  set; }
        
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