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
    ///LlmModel database entity
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AIModel))] //The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class AIModel : EntityBase<int>
    {
        /// <summary>
        /// code name
        /// </summary>
        [Required, MaxLength(50)]
        public string Alias { get; private set; }


        /// <summary>
        ///Model name (required)
        /// </summary>
        [Required, MaxLength(100)]
        public string ModelId { get; private set; }

        /// <summary>
        ///deployment name
        /// </summary>
        [MaxLength(150)]
        public string DeploymentName { get; private set; }

        /// <summary>
        ///Endpoint (optional)
        /// </summary>
        [MaxLength(250)]
        public string Endpoint { get; private set; }

        /// <summary>
        /// Type of model (required), for example: NeuCharAI, OpenAI, Azure OpenAI, HuggingFace
        /// </summary>
        [Required]
        public AiPlatform AiPlatform { get; private set; }

        /// <summary>
        /// model category
        /// </summary>
        [Required]
        public ConfigModelType ConfigModelType { get; private set; }

        /// <summary>
        ///OrganizationId (optional)
        /// </summary>
        [MaxLength(200)]
        public string OrganizationId { get; private set; }

        /// <summary>
        ///ApiKey (optional)
        /// </summary>
        [MaxLength(200)]
        public string ApiKey { get; private set; }

        /// <summary>
        ///ApiVersion (optional)
        /// </summary>
        [MaxLength(100)]
        public string ApiVersion { get; private set; }

        /// <summary>
        ///Note (optional)
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        ///MaxToken (optional)
        /// </summary>
        [Required, DefaultValue(0)]
        public int MaxToken { get; private set; }

        /// <summary>
        /// Whether to share
        /// </summary>
        [Required, DefaultValue(false)]
        public bool IsShared { get; private set; } = false;


        /// <summary>
        /// Whether to display
        /// </summary>
        [Required, DefaultValue(true)]
        public bool Show { get; private set; }


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
        public AIModel(AIModel_CreateOrEditRequest orEditRequest) : this(orEditRequest.DeploymentName, orEditRequest.Endpoint,
            orEditRequest.AiPlatform, orEditRequest.OrganizationId, orEditRequest.ApiKey, orEditRequest.ApiVersion, orEditRequest.Note,
            orEditRequest.MaxToken, orEditRequest.Alias, orEditRequest.ModelId,orEditRequest.ConfigModelType)
        {
        }

        /// <summary>
        /// Switch model visible state
        /// </summary>
        /// <param name="show"></param>
        /// <returns></returns>
        public AIModel SwitchShow(bool show)
        {
            Show = show;
            return this;
        }

        /// <summary>
        ///update model
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public AIModel Update(AIModel_CreateOrEditRequest request)
        {
            ModelId = request.ModelId;
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