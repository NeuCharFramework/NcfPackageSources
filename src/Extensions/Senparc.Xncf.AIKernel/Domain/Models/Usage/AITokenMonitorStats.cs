/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenMonitorStats.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    /// Aggregates token usage, call counts, latency, model breakdowns, and daily totals.
    /// </summary>
    [Serializable]
    public class AITokenMonitorStats
    {
        /// <summary>Gets or sets the total invocation count.</summary>
        public long TotalCalls { get; set; }

        /// <summary>Gets or sets the number of successful invocations.</summary>
        public long SuccessCount { get; set; }

        /// <summary>Gets or sets the number of failed invocations.</summary>
        public long ErrorCount { get; set; }

        /// <summary>Gets or sets the aggregate input-token count.</summary>
        public long TotalInputTokens { get; set; }

        /// <summary>Gets or sets the aggregate output-token count.</summary>
        public long TotalOutputTokens { get; set; }

        /// <summary>Gets or sets the aggregate token count.</summary>
        public long TotalTokens { get; set; }

        /// <summary>Gets or sets the average invocation duration in milliseconds.</summary>
        public double AverageDurationMs { get; set; }

        /// <summary>Gets or sets usage totals grouped by model.</summary>
        public List<AITokenModelStat> ByModel { get; set; } = new List<AITokenModelStat>();

        /// <summary>Gets or sets usage totals grouped by calendar day.</summary>
        public List<AITokenDailyStat> Daily { get; set; } = new List<AITokenDailyStat>();
    }

    /// <summary>Aggregates token usage for one configured model.</summary>
    [Serializable]
    public class AITokenModelStat
    {
        /// <summary>Gets or sets the model alias.</summary>
        public string ModelAlias { get; set; }

        /// <summary>Gets or sets the provider model identifier.</summary>
        public string ModelId { get; set; }

        /// <summary>Gets or sets the number of calls.</summary>
        public long Calls { get; set; }

        /// <summary>Gets or sets the input-token total.</summary>
        public long InputTokens { get; set; }

        /// <summary>Gets or sets the output-token total.</summary>
        public long OutputTokens { get; set; }

        /// <summary>Gets or sets the total-token total.</summary>
        public long TotalTokens { get; set; }
    }

    /// <summary>Aggregates token usage for one calendar day.</summary>
    [Serializable]
    public class AITokenDailyStat
    {
        /// <summary>Gets or sets the calendar date represented by the row.</summary>
        public DateTime Date { get; set; }

        /// <summary>Gets or sets the display-formatted calendar date.</summary>
        public string DateText { get; set; }

        /// <summary>Gets or sets the number of calls on the date.</summary>
        public long Calls { get; set; }

        /// <summary>Gets or sets the input-token total for the date.</summary>
        public long InputTokens { get; set; }

        /// <summary>Gets or sets the output-token total for the date.</summary>
        public long OutputTokens { get; set; }

        /// <summary>Gets or sets the total-token total for the date.</summary>
        public long TotalTokens { get; set; }
    }
}
