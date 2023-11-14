using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Models
{
    /// <summary>
    /// LlmModel 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(LlmModel))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class LlmModel : EntityBase<int>
    {
        /// <summary>
        /// 名称（必须）
        /// </summary>
        [Required, MaxLength(100)]
        public string Name { get; private set; }

        /// <summary>
        /// Endpoint（必须）
        /// </summary>
        [Required, MaxLength(250)]
        public string Endpoint { get; private set; }

        /// <summary>
        /// 模型的类型（必须）, 例如：OpenAI,Azure OpenAI,HuggingFace
        /// </summary>
        [Required, MaxLength(20)]
        public Constants.ModelTypeEnum ModelType { get; private set; }

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
        /// TextCompletionModelName（可选）
        /// </summary>
        [MaxLength(100)]
        public string TextCompletionModelName { get; private set; }

        /// <summary>
        /// TextEmbeddingModelName（可选）
        /// </summary>
        [MaxLength(100)]
        public string TextEmbeddingModelName { get; private set; }

        /// <summary>
        /// OtherModelName（可选）
        /// </summary>
        [MaxLength(100)]
        public string OtherModelName { get; private set; }

        /// <summary>
        /// 是否展示
        /// </summary>
        public bool Show { get; private set; }

        private LlmModel()
        {
        }

        public LlmModel(string name, string endpoint, string modelType, string organizationId, string apiKey,
            string apiVersion, string note, int maxToken, string textCompletionModelName, string textEmbeddingModelName,
            string otherModelName)
        {
            Name = name;
            Endpoint = endpoint;
            ModelType = Enum.Parse<Constants.ModelTypeEnum>(modelType);
            OrganizationId = organizationId;
            ApiKey = apiKey;
            ApiVersion = apiVersion;
            Note = note;
            MaxToken = maxToken;
            TextCompletionModelName = textCompletionModelName;
            TextEmbeddingModelName = textEmbeddingModelName;
            OtherModelName = otherModelName;
        }

        //public LlmModel(string name, string endpoint, string organizationId, string apiKey, string apiVersion, string note, int maxToken, string textCompletionModelName, string textEmbeddingModelName, string otherModelName)
        //{
        //    Name = name;
        //    Endpoint = endpoint;
        //    OrganizationId = organizationId;
        //    ApiKey = apiKey;
        //    ApiVersion = apiVersion;
        //    Note = note;
        //    MaxToken = maxToken;
        //    TextCompletionModelName = textCompletionModelName;
        //    TextEmbeddingModelName = textEmbeddingModelName;
        //    OtherModelName = otherModelName;
        //}

        public LlmModel(LlmModelDto llmModelDto)
        {
            Name = llmModelDto.Name;
            Endpoint = llmModelDto.Endpoint;
            OrganizationId = llmModelDto.OrganizationId;
            ApiKey = llmModelDto.ApiKey;
            ApiVersion = llmModelDto.ApiVersion;
            Note = llmModelDto.Note;
            MaxToken = llmModelDto.MaxToken;
            TextCompletionModelName = llmModelDto.TextCompletionModelName;
            TextEmbeddingModelName = llmModelDto.TextEmbeddingModelName;
            OtherModelName = llmModelDto.OtherModelName;
        }

        public LlmModel Switch(bool show)
        {
            Show = show;
            return this;
        }

        public LlmModel Update(string name, bool show)
        {
            this.Name = name;
            return Switch(show);
        }
    }
}