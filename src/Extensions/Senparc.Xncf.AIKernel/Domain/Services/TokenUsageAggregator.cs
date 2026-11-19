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
    ///Legacy comment translated to English.
    /// </summary>
    public static class TokenUsageAggregator
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string UnknownAlias = "(unknown)";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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
