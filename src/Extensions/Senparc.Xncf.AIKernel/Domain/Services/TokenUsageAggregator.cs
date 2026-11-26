/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TokenUsageAggregator.cs
    File description: Related implementation.

    Created by: Senparc - 20260905

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Senparc.Xncf.AIKernel.Domain.Models.Usage;
using Senparc.Xncf.AIKernel.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    /// <summary>
    /// Aggregates persisted AI token records into model-level usage summaries.
    /// </summary>
    public static class TokenUsageAggregator
    {
        /// <summary>
        /// Gets the alias used when a usage record has no model alias.
        /// </summary>
        public const string UnknownAlias = "(unknown)";

        /// <summary>
        /// Aggregates calls and token totals by model alias, tracking the latest usage time.
        /// Null or blank aliases use <see cref="UnknownAlias"/>.
        /// </summary>
        /// <param name="records">The token usage records to aggregate.</param>
        /// <returns>A case-insensitive map keyed by model alias.</returns>
        public static Dictionary<string, AITokenModelUsage> BuildModelUsageMap(IEnumerable<AITokenUsage> records)
        {
            var map = new Dictionary<string, AITokenModelUsage>(StringComparer.OrdinalIgnoreCase);
            if (records == null)
            {
                return map;
            }

            foreach (var record in records)
            {
                if (record == null)
                {
                    continue;
                }

                var key = string.IsNullOrWhiteSpace(record.ModelAlias) ? UnknownAlias : record.ModelAlias;
                if (!map.TryGetValue(key, out var usage))
                {
                    usage = new AITokenModelUsage();
                    map[key] = usage;
                }

                usage.Calls += 1;
                usage.InputTokens += record.InputTokens;
                usage.OutputTokens += record.OutputTokens;
                usage.TotalTokens += record.TotalTokens;
                if (!usage.LastUseTime.HasValue || record.AddTime > usage.LastUseTime.Value)
                {
                    usage.LastUseTime = record.AddTime;
                }
            }

            return map;
        }
    }
}
