/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptRange_DashboardResponse.cs
    文件功能描述：PromptRange 首页看板聚合响应对象
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：新增首页看板 DTO

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptRange_DashboardResponse
    {
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public int RecentDays { get; set; } = 14;

        public PromptRange_DashboardOverview Overview { get; set; } = new();
        public List<PromptRange_DashboardTrendPoint> UsageTrend { get; set; } = new();
        public List<PromptRange_DashboardPromptItem> TopPrompts { get; set; } = new();
        public List<PromptRange_DashboardRangeItem> TopRanges { get; set; } = new();
        public List<PromptRange_DashboardModelItem> TopModels { get; set; } = new();
        public List<PromptRange_DashboardRiskPromptItem> RiskPrompts { get; set; } = new();
        public List<PromptRange_DashboardInsightItem> Insights { get; set; } = new();
    }

    public class PromptRange_DashboardOverview
    {
        public int TotalRanges { get; set; }
        public int TotalPrompts { get; set; }
        public int DraftPrompts { get; set; }

        public int TotalResults { get; set; }
        public int ResultsToday { get; set; }
        public int ResultsLast7Days { get; set; }
        public int ActivePromptsLast7Days { get; set; }
        public int ActiveRangesLast7Days { get; set; }

        public decimal AvgFinalScore { get; set; } = -1;
        public decimal ScoreCoverageRate { get; set; }
        public long TotalTokens { get; set; }
        public double AvgLatencyMs { get; set; }
    }

    public class PromptRange_DashboardTrendPoint
    {
        public string Date { get; set; }
        public string DateLabel { get; set; }
        public int ResultCount { get; set; }
        public decimal AvgScore { get; set; } = -1;
        public long TokenCost { get; set; }
        public int ActivePromptCount { get; set; }
    }

    public class PromptRange_DashboardPromptItem
    {
        public int PromptId { get; set; }
        public int RangeId { get; set; }
        public string RangeAlias { get; set; }
        public string RangeName { get; set; }
        public string PromptName { get; set; }
        public string FullVersion { get; set; }
        public bool IsDraft { get; set; }
        public int UsageCount { get; set; }
        public decimal AvgScore { get; set; } = -1;
        public decimal MaxScore { get; set; } = -1;
        public DateTime AddTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public string PromptHash { get; set; }
    }

    public class PromptRange_DashboardRangeItem
    {
        public int RangeId { get; set; }
        public string RangeAlias { get; set; }
        public string RangeName { get; set; }
        public int PromptCount { get; set; }
        public int DraftPromptCount { get; set; }
        public int ActivePromptCount { get; set; }
        public int UsageCount { get; set; }
        public decimal AvgScore { get; set; } = -1;
        public decimal HealthScore { get; set; }
        public DateTime AddTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public string PromptHash { get; set; }
    }

    public class PromptRange_DashboardModelItem
    {
        public int ModelId { get; set; }
        public string Alias { get; set; }
        public string DeploymentName { get; set; }
        public int UsageCount { get; set; }
        public decimal AvgScore { get; set; } = -1;
        public long TokenCost { get; set; }
        public double AvgLatencyMs { get; set; }
    }

    public class PromptRange_DashboardRiskPromptItem
    {
        public int PromptId { get; set; }
        public int RangeId { get; set; }
        public string RangeAlias { get; set; }
        public string RangeName { get; set; }
        public string PromptName { get; set; }
        public string FullVersion { get; set; }
        public string RiskType { get; set; }
        public string RiskMessage { get; set; }
        public decimal AvgScore { get; set; } = -1;
        public int UsageCount { get; set; }
        public string PromptHash { get; set; }
    }

    public class PromptRange_DashboardInsightItem
    {
        public string Level { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}

