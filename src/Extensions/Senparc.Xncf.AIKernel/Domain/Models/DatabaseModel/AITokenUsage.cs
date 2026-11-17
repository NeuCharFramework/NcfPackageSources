/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: AITokenUsage.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

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
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AITokenUsage))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class AITokenUsage : EntityBase<int>
    {
        /// <summary>
        /// model code
        /// </summary>
        [MaxLength(50)]
        public string ModelAlias { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        [MaxLength(100)]
        public string ModelId { get; set; }

        /// <summary>
        /// deployment name
        /// </summary>
        [MaxLength(150)]
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
        [MaxLength(100)]
        public string Source { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(500)]
        public string ErrorNote { get; set; }
    }
}
