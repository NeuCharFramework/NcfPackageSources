/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenMonitor_Request.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using System;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AITokenUsage_GetListRequest : PagedRequest
    {
        /// <summary>
        /// model code
        /// </summary>
        public string ModelAlias { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AiPlatform? AiPlatform { get; set; }

        /// <summary>
        /// model category
        /// </summary>
        public ConfigModelType? ConfigModelType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// executestatus：0 success，1 failure
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AITokenMonitor_RunModelRequest
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int ModelId { get; set; }

        /// <summary>
        /// userPrompt
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string SystemMessage { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Source { get; set; } = "Monitor";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public float? Temperature { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AITokenMonitor_GetProgressRequest
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Guid RunId { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AITokenMonitor_RunModelResponse
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Guid RunId { get; set; }

        /// <summary>
        /// whethersuccess
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Output { get; set; }

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
        /// errorinformation
        /// </summary>
        public string Error { get; set; }
    }
}
