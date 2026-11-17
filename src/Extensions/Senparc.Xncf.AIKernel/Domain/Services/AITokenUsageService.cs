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
        public AITokenUsageService(IRepositoryBase<AITokenUsage> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
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

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        public async Task<Dictionary<string, AITokenModelUsage>> GetModelUsageMapAsync()
        {
            var all = await this.GetFullListAsync(z => z.Flag == false);
            return TokenUsageAggregator.BuildModelUsageMap(all);
        }
    }
}
