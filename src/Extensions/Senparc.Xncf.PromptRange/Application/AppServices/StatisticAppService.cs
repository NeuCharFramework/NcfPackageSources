/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：StatisticAppService.cs
    文件功能描述：StatisticAppService 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    /// <summary>
    /// 用于传送统计数据的接口服务
    /// TODO: 需要权限验证
    /// </summary>
    [ApiAuthorize("AdminOnly")]
    public class StatisticAppService : AppServiceBase
    {
        private readonly AIModelService _aiModelService;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly PromptResultService _promptResultService;

        public StatisticAppService(
            AIModelService aiModelService,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            PromptResultService promptResultService,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _aiModelService = aiModelService;
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _promptResultService = promptResultService;
        }


        [ApiBind]
        public async Task<StringAppResponse> TestAsync()
        {
            var response = await this.GetStringResponseAsync(
                delegate { return Task.FromResult("Service is Running"); }
            );
            return response;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptRange_DashboardResponse>> GetDashboardAsync(int recentDays = 14, int topN = 8)
        {
            return await this.GetResponseAsync<PromptRange_DashboardResponse>(
                async (response, logger) =>
                {
                    recentDays = Math.Clamp(recentDays, 7, 60);
                    topN = Math.Clamp(topN, 5, 20);

                    var now = SystemTime.Now.DateTime;
                    var today = now.Date;
                    var trendStartDate = today.AddDays(-(recentDays - 1));
                    var last7StartDate = today.AddDays(-6);
                    var previous7StartDate = today.AddDays(-13);
                    var previous7EndDate = today.AddDays(-7);

                    var ranges = await _promptRangeService.GetFullListAsync(z => true, z => z.Id, OrderingType.Ascending);
                    var promptItems = await _promptItemService.GetFullListAsync(z => true, z => z.Id, OrderingType.Ascending);
                    var promptResults = await _promptResultService.GetFullListAsync(z => true, z => z.Id, OrderingType.Ascending);
                    var models = await _aiModelService.GetFullListAsync(z => true, z => z.Id, OrderingType.Ascending);

                    var rangeById = ranges.ToDictionary(z => z.Id, z => z);
                    var promptById = promptItems.ToDictionary(z => z.Id, z => z);
                    var modelById = models.ToDictionary(z => z.Id, z => z);
                    var promptsByRangeId = promptItems
                        .GroupBy(z => z.RangeId)
                        .ToDictionary(g => g.Key, g => g.ToList());
                    var resultsByPromptId = promptResults
                        .GroupBy(z => z.PromptItemId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    var scoredResults = promptResults.Where(z => z.FinalScore >= 0).ToList();
                    var resultsLast7 = promptResults.Where(z => z.AddTime.Date >= last7StartDate).ToList();
                    var resultsToday = promptResults.Count(z => z.AddTime.Date == today);

                    var activePromptIdsLast7 = resultsLast7.Select(z => z.PromptItemId).Distinct().ToHashSet();
                    var activeRangeIdsLast7 = resultsLast7
                        .Select(z =>
                        {
                            if (promptById.TryGetValue(z.PromptItemId, out var prompt))
                            {
                                return prompt.RangeId;
                            }

                            return 0;
                        })
                        .Where(z => z > 0)
                        .Distinct()
                        .ToHashSet();

                    var dashboard = new PromptRange_DashboardResponse
                    {
                        GeneratedAt = now,
                        RecentDays = recentDays,
                        Overview = new PromptRange_DashboardOverview
                        {
                            TotalRanges = ranges.Count,
                            TotalPrompts = promptItems.Count,
                            DraftPrompts = promptItems.Count(z => z.IsDraft),
                            TotalResults = promptResults.Count,
                            ResultsToday = resultsToday,
                            ResultsLast7Days = resultsLast7.Count,
                            ActivePromptsLast7Days = activePromptIdsLast7.Count,
                            ActiveRangesLast7Days = activeRangeIdsLast7.Count,
                            AvgFinalScore = scoredResults.Count == 0
                                ? -1
                                : Math.Round(scoredResults.Average(z => z.FinalScore), 2),
                            ScoreCoverageRate = promptResults.Count == 0
                                ? 0
                                : Math.Round((decimal)scoredResults.Count * 100m / promptResults.Count, 2),
                            TotalTokens = promptResults.Sum(z => (long)Math.Max(0, z.TotalCostToken)),
                            AvgLatencyMs = promptResults.Count == 0
                                ? 0
                                : Math.Round(promptResults.Average(z => z.CostTime), 2)
                        }
                    };

                    #region Trend

                    var trendResultLookup = promptResults
                        .Where(z => z.AddTime.Date >= trendStartDate)
                        .GroupBy(z => z.AddTime.Date)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    for (var i = 0; i < recentDays; i++)
                    {
                        var date = trendStartDate.AddDays(i);
                        trendResultLookup.TryGetValue(date, out var dayResults);
                        dayResults ??= new List<PromptResult>();
                        var dayScoredResults = dayResults.Where(z => z.FinalScore >= 0).ToList();

                        dashboard.UsageTrend.Add(new PromptRange_DashboardTrendPoint
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            DateLabel = date.ToString("MM-dd"),
                            ResultCount = dayResults.Count,
                            AvgScore = dayScoredResults.Count == 0
                                ? -1
                                : Math.Round(dayScoredResults.Average(z => z.FinalScore), 2),
                            TokenCost = dayResults.Sum(z => (long)Math.Max(0, z.TotalCostToken)),
                            ActivePromptCount = dayResults.Select(z => z.PromptItemId).Distinct().Count()
                        });
                    }

                    #endregion

                    #region Prompt Ranking

                    var promptMetricList = new List<PromptRange_DashboardPromptItem>();
                    foreach (var promptItem in promptItems)
                    {
                        resultsByPromptId.TryGetValue(promptItem.Id, out var relatedResults);
                        relatedResults ??= new List<PromptResult>();
                        var relatedScoredResults = relatedResults.Where(z => z.FinalScore >= 0).ToList();

                        rangeById.TryGetValue(promptItem.RangeId, out var range);

                        promptMetricList.Add(new PromptRange_DashboardPromptItem
                        {
                            PromptId = promptItem.Id,
                            RangeId = promptItem.RangeId,
                            RangeAlias = range?.Alias,
                            RangeName = promptItem.RangeName,
                            PromptName = string.IsNullOrWhiteSpace(promptItem.NickName)
                                ? promptItem.FullVersion
                                : promptItem.NickName,
                            FullVersion = promptItem.FullVersion,
                            IsDraft = promptItem.IsDraft,
                            UsageCount = relatedResults.Count,
                            AvgScore = relatedScoredResults.Count == 0
                                ? -1
                                : Math.Round(relatedScoredResults.Average(z => z.FinalScore), 2),
                            MaxScore = relatedScoredResults.Count == 0
                                ? -1
                                : relatedScoredResults.Max(z => z.FinalScore),
                            AddTime = promptItem.AddTime,
                            LastRunTime = relatedResults.Count == 0
                                ? null
                                : relatedResults.Max(z => z.AddTime),
                            PromptHash = $"rangeId={promptItem.RangeId}&promptId={promptItem.Id}"
                        });
                    }

                    dashboard.TopPrompts = promptMetricList
                        .Where(z => z.UsageCount > 0)
                        .OrderByDescending(z => z.UsageCount)
                        .ThenByDescending(z => z.AvgScore)
                        .ThenByDescending(z => z.LastRunTime)
                        .Take(topN)
                        .ToList();

                    if (dashboard.TopPrompts.Count == 0)
                    {
                        dashboard.TopPrompts = promptMetricList
                            .OrderByDescending(z => z.AddTime)
                            .Take(topN)
                            .ToList();
                    }

                    #endregion

                    #region Range Ranking

                    var rangeMetricList = new List<PromptRange_DashboardRangeItem>();
                    foreach (var range in ranges)
                    {
                        promptsByRangeId.TryGetValue(range.Id, out var rangePrompts);
                        rangePrompts ??= new List<PromptItem>();
                        var rangeResults = new List<PromptResult>();

                        foreach (var prompt in rangePrompts)
                        {
                            if (resultsByPromptId.TryGetValue(prompt.Id, out var promptResultList) &&
                                promptResultList != null &&
                                promptResultList.Count > 0)
                            {
                                rangeResults.AddRange(promptResultList);
                            }
                        }

                        var scoredRangeResults = rangeResults.Where(z => z.FinalScore >= 0).ToList();
                        var promptCount = rangePrompts.Count;
                        var draftPromptCount = rangePrompts.Count(z => z.IsDraft);
                        var activePromptCount = rangeResults.Select(z => z.PromptItemId).Distinct().Count();

                        var avgScore = scoredRangeResults.Count == 0
                            ? -1
                            : Math.Round(scoredRangeResults.Average(z => z.FinalScore), 2);

                        decimal healthScore;
                        if (promptCount <= 0)
                        {
                            healthScore = 0;
                        }
                        else
                        {
                            var draftRate = (decimal)draftPromptCount / promptCount;
                            var activeRate = (decimal)activePromptCount / promptCount;
                            var scoreRate = avgScore < 0 ? 0.4m : Math.Min(1m, avgScore / 10m);
                            healthScore = Math.Round((scoreRate * 0.6m + activeRate * 0.3m + (1m - draftRate) * 0.1m) * 100m, 1);
                        }

                        rangeMetricList.Add(new PromptRange_DashboardRangeItem
                        {
                            RangeId = range.Id,
                            RangeAlias = range.Alias,
                            RangeName = range.RangeName,
                            PromptCount = promptCount,
                            DraftPromptCount = draftPromptCount,
                            ActivePromptCount = activePromptCount,
                            UsageCount = rangeResults.Count,
                            AvgScore = avgScore,
                            HealthScore = healthScore,
                            AddTime = range.AddTime,
                            LastRunTime = rangeResults.Count == 0 ? null : rangeResults.Max(z => z.AddTime),
                            PromptHash = $"rangeId={range.Id}"
                        });
                    }

                    dashboard.TopRanges = rangeMetricList
                        .Where(z => z.PromptCount > 0)
                        .OrderByDescending(z => z.UsageCount)
                        .ThenByDescending(z => z.HealthScore)
                        .ThenByDescending(z => z.PromptCount)
                        .Take(topN)
                        .ToList();

                    if (dashboard.TopRanges.Count == 0)
                    {
                        dashboard.TopRanges = rangeMetricList
                            .OrderByDescending(z => z.AddTime)
                            .Take(topN)
                            .ToList();
                    }

                    #endregion

                    #region Model Ranking

                    dashboard.TopModels = promptResults
                        .Where(z => z.LlmModelId > 0)
                        .GroupBy(z => z.LlmModelId)
                        .Select(group =>
                        {
                            var scoredModelResults = group.Where(z => z.FinalScore >= 0).ToList();
                            modelById.TryGetValue(group.Key, out var model);

                            return new PromptRange_DashboardModelItem
                            {
                                ModelId = group.Key,
                                Alias = model?.Alias,
                                DeploymentName = model?.DeploymentName,
                                UsageCount = group.Count(),
                                AvgScore = scoredModelResults.Count == 0
                                    ? -1
                                    : Math.Round(scoredModelResults.Average(z => z.FinalScore), 2),
                                TokenCost = group.Sum(z => (long)Math.Max(0, z.TotalCostToken)),
                                AvgLatencyMs = Math.Round(group.Average(z => z.CostTime), 2)
                            };
                        })
                        .OrderByDescending(z => z.UsageCount)
                        .ThenByDescending(z => z.AvgScore)
                        .Take(topN)
                        .ToList();

                    #endregion

                    #region Risk Prompts

                    var riskCandidates = new List<(int severity, PromptRange_DashboardRiskPromptItem item)>();

                    foreach (var promptItem in promptMetricList)
                    {
                        var riskType = string.Empty;
                        var riskMessage = string.Empty;
                        var severity = 2;

                        if (promptItem.UsageCount > 0 && promptItem.AvgScore >= 0 && promptItem.AvgScore < 6)
                        {
                            riskType = "低分";
                            riskMessage = $"平均分 {promptItem.AvgScore:0.##}，建议优化 Prompt 结构或模型参数后重测。";
                            severity = 0;
                        }
                        else if (promptItem.UsageCount > 0 && promptItem.AvgScore < 0)
                        {
                            riskType = "未评分";
                            riskMessage = "已有打靶结果但尚未完成评分，难以判断优劣。";
                            severity = 1;
                        }
                        else if (promptItem.UsageCount == 0 && !promptItem.IsDraft)
                        {
                            riskType = "未打靶";
                            riskMessage = "已存在正式靶道但没有任何打靶结果，缺少基线数据。";
                            severity = 1;
                        }
                        else if (promptItem.UsageCount > 0 && promptItem.IsDraft)
                        {
                            riskType = "草稿在用";
                            riskMessage = "草稿版本已被调用，建议沉淀稳定版后继续协作。";
                            severity = 2;
                        }

                        if (string.IsNullOrEmpty(riskType))
                        {
                            continue;
                        }

                        riskCandidates.Add((severity, new PromptRange_DashboardRiskPromptItem
                        {
                            PromptId = promptItem.PromptId,
                            RangeId = promptItem.RangeId,
                            RangeAlias = promptItem.RangeAlias,
                            RangeName = promptItem.RangeName,
                            PromptName = promptItem.PromptName,
                            FullVersion = promptItem.FullVersion,
                            RiskType = riskType,
                            RiskMessage = riskMessage,
                            AvgScore = promptItem.AvgScore,
                            UsageCount = promptItem.UsageCount,
                            PromptHash = promptItem.PromptHash
                        }));
                    }

                    dashboard.RiskPrompts = riskCandidates
                        .OrderBy(z => z.severity)
                        .ThenBy(z => z.item.AvgScore < 0 ? decimal.MaxValue : z.item.AvgScore)
                        .ThenByDescending(z => z.item.UsageCount)
                        .Select(z => z.item)
                        .Take(topN)
                        .ToList();

                    #endregion

                    #region Insights

                    var previous7Count = promptResults.Count(z =>
                        z.AddTime.Date >= previous7StartDate &&
                        z.AddTime.Date <= previous7EndDate);
                    var recent7Count = dashboard.Overview.ResultsLast7Days;
                    var growthRate = previous7Count == 0
                        ? (recent7Count > 0 ? 100D : 0D)
                        : (recent7Count - previous7Count) * 100D / previous7Count;

                    var growthLevel = growthRate <= -10 ? "warning" : growthRate >= 10 ? "success" : "info";
                    var growthDescription = previous7Count == 0
                        ? $"近 7 天打靶 {recent7Count} 次，上一周期无可比数据。"
                        : $"近 7 天打靶 {recent7Count} 次，较前 7 天{(growthRate >= 0 ? "增长" : "下降")} {Math.Abs(growthRate):0.#}%（前 7 天：{previous7Count} 次）。";

                    dashboard.Insights.Add(new PromptRange_DashboardInsightItem
                    {
                        Level = growthLevel,
                        Title = "活跃度趋势",
                        Description = growthDescription
                    });

                    var scoreCoverage = dashboard.Overview.ScoreCoverageRate;
                    dashboard.Insights.Add(new PromptRange_DashboardInsightItem
                    {
                        Level = scoreCoverage >= 75 ? "success" : scoreCoverage < 40 ? "warning" : "info",
                        Title = "评分覆盖率",
                        Description = $"当前评分覆盖率 {scoreCoverage:0.##}%（已评分结果 / 全部结果），建议保持在 75% 以上。"
                    });

                    var bestRange = dashboard.TopRanges.OrderByDescending(z => z.HealthScore).FirstOrDefault();
                    if (bestRange != null)
                    {
                        var bestRangeName = string.IsNullOrWhiteSpace(bestRange.RangeAlias)
                            ? bestRange.RangeName
                            : $"{bestRange.RangeAlias}（{bestRange.RangeName}）";

                        dashboard.Insights.Add(new PromptRange_DashboardInsightItem
                        {
                            Level = "success",
                            Title = "当前最佳靶场",
                            Description = $"{bestRangeName} 健康度 {bestRange.HealthScore:0.#}，累计打靶 {bestRange.UsageCount} 次。"
                        });
                    }

                    if (dashboard.TopModels.Count > 0 && dashboard.Overview.TotalResults > 0)
                    {
                        var mainModel = dashboard.TopModels[0];
                        var mainModelName = !string.IsNullOrWhiteSpace(mainModel.Alias)
                            ? mainModel.Alias
                            : (!string.IsNullOrWhiteSpace(mainModel.DeploymentName)
                                ? mainModel.DeploymentName
                                : $"Model#{mainModel.ModelId}");

                        var mainModelRate = (decimal)mainModel.UsageCount * 100m / dashboard.Overview.TotalResults;

                        dashboard.Insights.Add(new PromptRange_DashboardInsightItem
                        {
                            Level = mainModelRate > 70 ? "warning" : "info",
                            Title = "模型集中度",
                            Description = $"{mainModelName} 承载 {mainModelRate:0.#}% 的打靶请求，建议关注单模型依赖风险。"
                        });
                    }

                    logger.SaveLogs(
                        $"Dashboard 生成完成：Ranges={dashboard.Overview.TotalRanges}, Prompts={dashboard.Overview.TotalPrompts}, Results={dashboard.Overview.TotalResults}");

                    #endregion

                    return dashboard;
                });
        }

        [ApiBind]
        public async Task<AppResponseBase<Statistics_TodayTacticResponse>> TodayTacticStatisticAsync([FromQuery] string today)
        {
            var response = await this.GetResponseAsync<Statistics_TodayTacticResponse>(
                async (resp, logger) =>
                {
                    int cnt = await _promptItemService.GetCountAsync(
                        p => p.RangeName.StartsWith(today)
                    );

                    return new Statistics_TodayTacticResponse(cnt);
                });
            return response;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptItem_HistoryScoreResponse>> GetHistoryScoreAsync(int promptItemId)
        {
            return await this.GetResponseAsync<PromptItem_HistoryScoreResponse>(
                async (response, logger) =>
                {
                    logger.SaveLogs($"传入ID为{promptItemId}");

                    return await _promptItemService.GetHistoryScoreAsync(promptItemId);
                });
        }


        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<Statistic_TodayTacticResponse>> GetLineChartDataAsync(int promptItemId, bool isAvg = true)
        {
            return await this.GetResponseAsync<Statistic_TodayTacticResponse>(
                async (response, logger) =>
                {
                    logger.SaveLogs($"查询三维折线图，传入ID为{promptItemId}");

                    return await _promptItemService.GetLineChartDataAsync(promptItemId, isAvg);
                });
        }
    }
}
