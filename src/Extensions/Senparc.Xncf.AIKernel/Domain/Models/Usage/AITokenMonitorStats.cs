/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenMonitorStats.cs
    文件功能描述：Token 监控聚合统计
    
    创建标识：Senparc - 20260904

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    /// Token 监控聚合统计
    /// </summary>
    [Serializable]
    public class AITokenMonitorStats
    {
        /// <summary>
        /// 记录总数（调用次数）
        /// </summary>
        public long TotalCalls { get; set; }

        /// <summary>
        /// 成功次数
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        /// 失败次数
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// 输入（Prompt）Token 合计
        /// </summary>
        public long TotalInputTokens { get; set; }

        /// <summary>
        /// 输出（Completion）Token 合计
        /// </summary>
        public long TotalOutputTokens { get; set; }

        /// <summary>
        /// 总 Token 合计
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// 平均耗时（毫秒）
        /// </summary>
        public double AverageDurationMs { get; set; }

        /// <summary>
        /// 按模型聚合
        /// </summary>
        public List<AITokenModelStat> ByModel { get; set; } = new List<AITokenModelStat>();

        /// <summary>
        /// 最近 N 天按天聚合
        /// </summary>
        public List<AITokenDailyStat> Daily { get; set; } = new List<AITokenDailyStat>();
    }

    /// <summary>
    /// 按模型聚合的 Token 统计
    /// </summary>
    [Serializable]
    public class AITokenModelStat
    {
        /// <summary>
        /// 模型代号
        /// </summary>
        public string ModelAlias { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// 调用次数
        /// </summary>
        public long Calls { get; set; }

        /// <summary>
        /// 输入（Prompt）Token 合计
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// 输出（Completion）Token 合计
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// 总 Token 合计
        /// </summary>
        public long TotalTokens { get; set; }
    }

    /// <summary>
    /// 按天聚合的 Token 统计
    /// </summary>
    [Serializable]
    public class AITokenDailyStat
    {
        /// <summary>
        /// 统计日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 日期字符串（yyyy-MM-dd）
        /// </summary>
        public string DateText { get; set; }

        /// <summary>
        /// 调用次数
        /// </summary>
        public long Calls { get; set; }

        /// <summary>
        /// 输入（Prompt）Token 合计
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// 输出（Completion）Token 合计
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// 总 Token 合计
        /// </summary>
        public long TotalTokens { get; set; }
    }
}
