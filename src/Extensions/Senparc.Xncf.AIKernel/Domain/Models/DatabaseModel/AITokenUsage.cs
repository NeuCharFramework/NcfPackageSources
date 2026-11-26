/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenUsage.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Domain.Models;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AIKernel.Models
{
    /// <summary>
    /// Stores token, latency, status, and source diagnostics for one AI invocation.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AITokenUsage))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class AITokenUsage : EntityBase<int>
    {
        /// <summary>
        /// Gets or sets the configured model alias.
        /// </summary>
        [MaxLength(50)]
        public string ModelAlias { get; set; }

        /// <summary>
        /// Gets or sets the provider model identifier.
        /// </summary>
        [MaxLength(100)]
        public string ModelId { get; set; }

        /// <summary>
        /// Gets or sets the provider deployment name.
        /// </summary>
        [MaxLength(150)]
        public string DeploymentName { get; set; }

        /// <summary>
        /// Gets or sets the AI platform that processed the invocation.
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// Gets or sets the model capability category.
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }

        /// <summary>
        /// Gets or sets the number of input/prompt tokens.
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of output/completion tokens.
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// Gets or sets the total number of input and output tokens.
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of input tokens served from provider cache.
        /// </summary>
        public long CachedInputTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of reasoning tokens reported by the provider.
        /// </summary>
        public long ReasoningTokens { get; set; }

        /// <summary>
        /// Gets or sets the invocation duration in milliseconds.
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the execution status code, where zero represents success.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the logical source that initiated the invocation.
        /// </summary>
        [MaxLength(100)]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the bounded provider or execution error description.
        /// </summary>
        [MaxLength(500)]
        public string ErrorNote { get; set; }
    }
}
