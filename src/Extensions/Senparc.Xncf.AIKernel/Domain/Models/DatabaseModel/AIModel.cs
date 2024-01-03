using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.AI;
using Senparc.Xncf.AIKernel.OHS.Local.PL;

namespace Senparc.Xncf.AIKernel.Models
{
    /// <summary>
    /// LlmModel 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AIModel))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AIModel : EntityBase<int>
    {
        /// <summary>
        /// 代号
        /// </summary>
        [Required, MaxLength(50)]
        public string Alias { get; private set; }

        /// <summary>
        /// 模型名称（必须）
        /// </summary>
        [Required, MaxLength(100)]
        public string DeploymentName { get; private set; }

        /// <summary>
        /// Endpoint（必须）
        /// </summary>
        [Required, MaxLength(250)]
        public string Endpoint { get; private set; }

        /// <summary>
        /// 模型的类型（必须）, 例如：NeuCharAI, OpenAI, Azure OpenAI, HuggingFace
        /// </summary>
        [Required]
        public AiPlatform AiPlatform { get; private set; }

        /// <summary>
        /// OrganizationId（可选）
        /// </summary>
        [MaxLength(200)]
        public string OrganizationId { get; private set; }

        /// <summary>
        /// ApiKey（可选）
        /// </summary>
        [MaxLength(200)]
        public string ApiKey { get; private set; }

        /// <summary>
        /// ApiVersion（可选）
        /// </summary>
        [MaxLength(100)]
        public string ApiVersion { get; private set; }

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        /// MaxToken（可选）
        /// </summary>
        [Required, DefaultValue(0)]
        public int MaxToken { get; private set; }

        /// <summary>
        /// 是否共享
        /// </summary>
        public bool IsShared { get; private set; } = false;


        /// <summary>
        /// 是否展示
        /// </summary>
        public bool Show { get; private set; }

        private AIModel(string alias)
        {
            Alias = alias;
        }

        public AIModel(string deploymentName, string endpoint, AiPlatform aiPlatform, string organizationId, string apiKey,
            string apiVersion, string note, int maxToken, string alias)
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
        }

        public AIModel(AIModelDto llmModelDto)
        {
            Alias = llmModelDto.Alias;
            DeploymentName = llmModelDto.Name;
            Endpoint = llmModelDto.Endpoint;
            AiPlatform = llmModelDto.AiPlatform;
            OrganizationId = llmModelDto.OrganizationId;
            ApiKey = llmModelDto.ApiKey;
            ApiVersion = llmModelDto.ApiVersion;
            Note = llmModelDto.Note;
            MaxToken = llmModelDto.MaxToken;


            // TextCompletionModelName = llmModelDto.TextCompletionModelName;
            // TextEmbeddingModelName = llmModelDto.TextEmbeddingModelName;
            // OtherModelName = llmModelDto.OtherModelName;
        }

        public AIModel(AIModel_CreateRequest request) : this(request.DeploymentName, request.Endpoint,
            request.AiPlatform, request.OrganizationId, request.ApiKey, request.ApiVersion, request.Note,
            request.MaxToken, request.Alias)
        {
        }

        public AIModel Switch(bool show)
        {
            Show = show;
            return this;
        }

        public AIModel Update(string alias, bool show, bool isShared)
        {
            this.Alias = alias;
            this.IsShared = isShared;
            return Switch(show);
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