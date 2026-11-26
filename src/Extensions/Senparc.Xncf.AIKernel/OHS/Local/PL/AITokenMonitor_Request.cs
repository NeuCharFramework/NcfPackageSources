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
    /// Defines filters for querying persisted AI token-usage records.
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
        /// Gets or sets the AI platform filter.
        /// </summary>
        public AiPlatform? AiPlatform { get; set; }

        /// <summary>
        /// model category
        /// </summary>
        public ConfigModelType? ConfigModelType { get; set; }

        /// <summary>
        /// Gets or sets the source subsystem filter.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// executestatus：0 success，1 failure
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets the inclusive start of the usage time range.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the inclusive end of the usage time range.
        /// </summary>
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// Defines the input required to run one model through the token monitor.
    /// </summary>
    public class AITokenMonitor_RunModelRequest
    {
        /// <summary>
        /// Gets or sets the AI model identifier.
        /// </summary>
        public int ModelId { get; set; }

        /// <summary>
        /// userPrompt
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// Gets or sets the system instruction sent to the model.
        /// </summary>
        public string SystemMessage { get; set; }

        /// <summary>
        /// Gets or sets the source label recorded with the run.
        /// </summary>
        public string Source { get; set; } = "Monitor";

        /// <summary>
        /// Gets or sets the maximum completion-token budget.
        /// </summary>
        public int MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the optional sampling temperature.
        /// </summary>
        public float? Temperature { get; set; }
    }

    /// <summary>
    /// Defines the request used to poll a token-monitor run.
    /// </summary>
    public class AITokenMonitor_GetProgressRequest
    {
        /// <summary>
        /// Gets or sets the run identifier to query.
        /// </summary>
        public Guid RunId { get; set; }
    }

    /// <summary>
    /// Represents the output and token diagnostics of a monitored model run.
    /// </summary>
    public class AITokenMonitor_RunModelResponse
    {
        /// <summary>
        /// Gets or sets the monitored run identifier.
        /// </summary>
        public Guid RunId { get; set; }

        /// <summary>
        /// whethersuccess
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the generated output text.
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
        /// Gets or sets the reasoning-token count.
        /// </summary>
        public long ReasoningTokens { get; set; }

        /// <summary>
        /// Gets or sets the elapsed execution time in milliseconds.
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// errorinformation
        /// </summary>
        public string Error { get; set; }
    }
}
