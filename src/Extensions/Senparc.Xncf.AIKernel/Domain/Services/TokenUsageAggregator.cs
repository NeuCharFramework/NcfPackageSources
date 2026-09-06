/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TokenUsageAggregator.cs
    文件功能描述：Token 使用记录按模型聚合工具
    
    创建标识：Senparc - 20260905

----------------------------------------------------------------*/

using Senparc.Xncf.AIKernel.Domain.Models.Usage;
using Senparc.Xncf.AIKernel.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    /// <summary>
    /// Token 使用记录按模型聚合工具
    /// </summary>
    public static class TokenUsageAggregator
    {
        /// <summary>
        /// 未知（空）模型代号的分组键
        /// </summary>
        public const string UnknownAlias = "(unknown)";

        /// <summary>
        /// 将 Token 使用记录按模型代号聚合（模型代号不区分大小写）。
        /// </summary>
        /// <param name="records">Token 使用记录（调用方负责过滤已删除数据）</param>
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
