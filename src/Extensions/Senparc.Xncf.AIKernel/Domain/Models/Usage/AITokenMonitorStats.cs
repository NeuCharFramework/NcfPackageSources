/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenMonitorStats.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public class AITokenMonitorStats
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long TotalCalls { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long TotalInputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long TotalOutputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public double AverageDurationMs { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<AITokenModelStat> ByModel { get; set; } = new List<AITokenModelStat>();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<AITokenDailyStat> Daily { get; set; } = new List<AITokenDailyStat>();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public class AITokenModelStat
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
        public long Calls { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long TotalTokens { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public class AITokenDailyStat
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string DateText { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long Calls { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long TotalTokens { get; set; }
    }
}
