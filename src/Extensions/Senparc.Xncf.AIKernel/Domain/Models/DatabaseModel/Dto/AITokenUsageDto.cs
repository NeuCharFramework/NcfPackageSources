/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenUsageDto.cs
    File description: AITokenUsageDto related implementation

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Models;
using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto
{
    public class AITokenUsageDto : DtoBase
    {
        /// <summary>
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// model code
        /// </summary>
        public string ModelAlias { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// deployment name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// model category
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }

        /// <summary>
        /// input (Prompt) token count
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// output (Completion) token count
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// total token count
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// cached input token count
        /// </summary>
        public long CachedInputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long ReasoningTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// executestatus：0 success，1 failure
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// errorinformation
        /// </summary>
        public string ErrorNote { get; set; }

        public AITokenUsageDto()
        {
        }

        public AITokenUsageDto(AITokenUsage aiTokenUsage)
        {
            Id = aiTokenUsage.Id;
            ModelAlias = aiTokenUsage.ModelAlias;
            ModelId = aiTokenUsage.ModelId;
            DeploymentName = aiTokenUsage.DeploymentName;
            AiPlatform = aiTokenUsage.AiPlatform;
            ConfigModelType = aiTokenUsage.ConfigModelType;
            InputTokens = aiTokenUsage.InputTokens;
            OutputTokens = aiTokenUsage.OutputTokens;
            TotalTokens = aiTokenUsage.TotalTokens;
            CachedInputTokens = aiTokenUsage.CachedInputTokens;
            ReasoningTokens = aiTokenUsage.ReasoningTokens;
            DurationMs = aiTokenUsage.DurationMs;
            Status = aiTokenUsage.Status;
            Source = aiTokenUsage.Source;
            ErrorNote = aiTokenUsage.ErrorNote;

            AddTime = aiTokenUsage.AddTime;
            LastUpdateTime = aiTokenUsage.LastUpdateTime;
            TenantId = aiTokenUsage.TenantId;
            Remark = aiTokenUsage.Remark;
            AdminRemark = aiTokenUsage.AdminRemark;
        }
    }
}
