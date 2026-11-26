/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenUsageService.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.AI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    public class AITokenUsageService : ServiceBase<AITokenUsage>
    {
        /// <summary>
        /// Initializes token usage persistence.
        /// </summary>
        /// <param name="repo">The token usage repository.</param>
        /// <param name="serviceProvider">The dependency provider.</param>
        public AITokenUsageService(IRepositoryBase<AITokenUsage> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Normalizes and persists one token-usage record.
        /// </summary>
        /// <param name="modelAlias">The model alias.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="deploymentName">The deployment name.</param>
        /// <param name="aiPlatform">The AI platform.</param>
        /// <param name="configModelType">The model capability type.</param>
        /// <param name="snapshot">The token usage snapshot.</param>
        /// <param name="durationMs">The operation duration.</param>
        /// <param name="success">Whether the operation succeeded.</param>
        /// <param name="source">The usage source.</param>
        /// <param name="errorNote">The optional error note.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="occurredAt">The occurrence timestamp.</param>
        /// <returns>The persisted usage DTO.</returns>
        public async Task<AITokenUsageDto> RecordAsync(
            string modelAlias,
            string modelId,
            string deploymentName,
            AiPlatform aiPlatform,
            ConfigModelType configModelType,
            AITokenUsageSnapshot snapshot,
            int durationMs,
            bool success,
            string source = null,
            string errorNote = null,
            int tenantId = 0,
            DateTime? occurredAt = null)
        {
            var normalized = (snapshot ?? AITokenUsageSnapshot.Empty);
            normalized.Normalize();

            var entity = new AITokenUsage
            {
                ModelAlias = Truncate(modelAlias, 50),
                ModelId = Truncate(modelId, 100),
                DeploymentName = Truncate(deploymentName, 150),
                AiPlatform = aiPlatform,
                ConfigModelType = configModelType,
                InputTokens = normalized.InputTokens,
                OutputTokens = normalized.OutputTokens,
                TotalTokens = normalized.TotalTokens,
                CachedInputTokens = normalized.CachedInputTokens,
                ReasoningTokens = normalized.ReasoningTokens,
                DurationMs = Math.Max(0, durationMs),
                Status = success ? 0 : 1,
                Source = Truncate(source, 100),
                ErrorNote = string.IsNullOrWhiteSpace(errorNote) ? null : Truncate(errorNote, 500),
                TenantId = tenantId,
                AddTime = occurredAt ?? DateTime.Now
            };

            await this.SaveObjectAsync(entity);
            return new AITokenUsageDto(entity);
        }

        /// <summary>
        /// Bounds a usage metadata string to the database column limit.
        /// </summary>
        /// <param name="value">The source string.</param>
        /// <param name="maxLength">The maximum retained length.</param>
        /// <returns>The original or truncated string.</returns>
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        /// <summary>
        /// Builds the optional model/platform/source/time filter for usage queries.
        /// </summary>
        /// <param name="request">The usage list filter request.</param>
        /// <returns>An EF-translatable usage predicate.</returns>
        public Expression<Func<AITokenUsage, bool>> GetListWhere(AITokenUsage_GetListRequest request)
        {
            SenparcExpressionHelper<AITokenUsage> helper = new();
            helper.ValueCompare
                .AndAlso(!request.ModelAlias.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.ModelAlias, request.ModelAlias))
                .AndAlso(!request.ModelId.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.ModelId, request.ModelId))
                .AndAlso(request.AiPlatform.HasValue, z => z.AiPlatform == request.AiPlatform)
                .AndAlso(request.ConfigModelType.HasValue, z => z.ConfigModelType == request.ConfigModelType)
                .AndAlso(!request.Source.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.Source, request.Source))
                .AndAlso(request.Status.HasValue, z => z.Status == request.Status)
                .AndAlso(request.StartTime.HasValue, z => z.AddTime >= request.StartTime)
                .AndAlso(request.EndTime.HasValue, z => z.AddTime <= request.EndTime)
                ;
            return helper.BuildWhereExpression();
        }

        /// <summary>
        /// Aggregates persisted usage records into overall, per-model, and daily statistics.
        /// </summary>
        /// <param name="dailyDays">The number of recent daily buckets to produce.</param>
        /// <returns>Aggregated token monitor statistics.</returns>
        public async Task<AITokenMonitorStats> GetStatsAsync(int dailyDays = 7)
        {
            var all = await this.GetFullListAsync(z => z.Flag == false, "AddTime desc");

            var stats = new AITokenMonitorStats
            {
                TotalCalls = all.Count,
                SuccessCount = all.Count(z => z.Status == 0),
                ErrorCount = all.Count(z => z.Status != 0),
                TotalInputTokens = all.Sum(z => z.InputTokens),
                TotalOutputTokens = all.Sum(z => z.OutputTokens),
                TotalTokens = all.Sum(z => z.TotalTokens),
                AverageDurationMs = all.Count > 0 ? all.Average(z => z.DurationMs) : 0
            };

            var byModel = all
                .GroupBy(z => z.ModelAlias ?? "(unknown)")
                .Select(g => new AITokenModelStat
                {
                    ModelAlias = g.Key,
                    ModelId = g.FirstOrDefault()?.ModelId,
                    Calls = g.Count(),
                    InputTokens = g.Sum(x => x.InputTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens)
                })
                .OrderByDescending(z => z.TotalTokens)
                .ToList();
            stats.ByModel = byModel;

            var today = DateTime.Now.Date;
            for (int i = Math.Max(0, dailyDays - 1); i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var dayEnd = day.AddDays(1);
                var dayList = all.Where(z => z.AddTime >= day && z.AddTime < dayEnd).ToList();
                stats.Daily.Add(new AITokenDailyStat
                {
                    Date = day,
                    DateText = day.ToString("yyyy-MM-dd"),
                    Calls = dayList.Count,
                    InputTokens = dayList.Sum(z => z.InputTokens),
                    OutputTokens = dayList.Sum(z => z.OutputTokens),
                    TotalTokens = dayList.Sum(z => z.TotalTokens)
                });
            }

            return stats;
        }

        /// <summary>
        /// Builds a case-insensitive model-alias usage map from non-deleted persisted records.
        /// </summary>
        /// <returns>Aggregated usage keyed by model alias.</returns>
        public async Task<Dictionary<string, AITokenModelUsage>> GetModelUsageMapAsync()
        {
            var all = await this.GetFullListAsync(z => z.Flag == false);
            return TokenUsageAggregator.BuildModelUsageMap(all);
        }
    }
}
