/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupHistoryAppService.cs
    文件功能描述：ChatGroupHistoryAppService 服务逻辑
    
    
    创建标识：Senparc - 20241017
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    [ApiAuthorize]

    public class ChatGroupHistoryAppService : AppServiceBase
    {
        private readonly ChatGroupHistoryService _chatGroupHistoryService;

        public ChatGroupHistoryAppService(IServiceProvider serviceProvider, ChatGroupHistoryService chatGroupHistoryService) : base(serviceProvider)
        {
            this._chatGroupHistoryService = chatGroupHistoryService;
        }

        [ApiBind]
        public async Task<AppResponseBase<ChatGroupHistory_GetListResponse>> GetList(int chatTaskId, int nextHistoryId, int pageIndex, int pageSize)
        {
            return await this.GetResponseAsync<ChatGroupHistory_GetListResponse>(async (response, logger) =>
            {
                var list = await this._chatGroupHistoryService.
                        GetObjectListAsync(pageIndex, pageSize,
                        z => z.Id > nextHistoryId && z.ChatTaskId == chatTaskId,
                        z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);

                var result = new ChatGroupHistory_GetListResponse()
                {
                    ChatGroupHistories = this._chatGroupHistoryService.Mapping<ChatGroupHistoryDto>(list)
                };

                foreach (var historyDto in result.ChatGroupHistories)
                {
                    historyDto.Message = _chatGroupHistoryService.GetRawMessage(historyDto.Message);
                    PopulateUsageFields(historyDto);
                }

                return result;
            });
        }

        [ApiBind]
        public async Task<AppResponseBase<ChatGroupHistory_GetUsageAnalyticsResponse>> GetUsageAnalytics(
            int chatTaskId,
            int? agentTemplateId = null,
            string startTime = null,
            string endTime = null)
        {
            return await this.GetResponseAsync<ChatGroupHistory_GetUsageAnalyticsResponse>(async (response, logger) =>
            {
                var histories = await _chatGroupHistoryService.GetFullListAsync(
                    z => z.ChatTaskId == chatTaskId,
                    z => z.Id,
                    Ncf.Core.Enums.OrderingType.Ascending);

                var allItems = histories
                    .Select((history, index) =>
                    {
                        var parsed = ChatUsageRemarkCodec.TryDecodeMessage(history.AdminRemark, out var usage)
                            ? usage
                            : new ChatUsageSnapshot();

                        var round = parsed.RoundIndex > 0 ? parsed.RoundIndex : index + 1;
                        var totalTokens = parsed.TotalTokens > 0 ? parsed.TotalTokens : parsed.PromptTokens + parsed.CompletionTokens;

                        return new
                        {
                            History = history,
                            Usage = new ChatUsageSnapshot
                            {
                                PromptTokens = parsed.PromptTokens,
                                CompletionTokens = parsed.CompletionTokens,
                                TotalTokens = totalTokens,
                                ResponseMilliseconds = parsed.ResponseMilliseconds,
                                RoundIndex = round,
                                ResponseId = parsed.ResponseId
                            }
                        };
                    })
                    .ToList();

                var filteredItems = allItems
                    .Where(z => !agentTemplateId.HasValue || z.History.FromAgentTemplateId == agentTemplateId.Value)
                    .ToList();

                if (TryParseDateTime(startTime, out var startDateTime))
                {
                    filteredItems = filteredItems.Where(z => z.History.AddTime >= startDateTime).ToList();
                }

                if (TryParseDateTime(endTime, out var endDateTime))
                {
                    filteredItems = filteredItems.Where(z => z.History.AddTime <= endDateTime).ToList();
                }

                var agentsTemplateService = GetRequiredService<AgentsTemplateService>();
                var agentIds = filteredItems
                    .Where(z => z.History.FromAgentTemplateId.HasValue)
                    .Select(z => z.History.FromAgentTemplateId!.Value)
                    .Distinct()
                    .ToList();

                var agentNameMap = agentIds.Count == 0
                    ? new Dictionary<int, string>()
                    : (await agentsTemplateService.GetFullListAsync(z => agentIds.Contains(z.Id)))
                        .ToDictionary(z => z.Id, z => z.Name ?? $"Agent-{z.Id}");

                var responseMilliseconds = filteredItems
                    .Select(z => z.Usage.ResponseMilliseconds)
                    .Where(z => z > 0)
                    .OrderBy(z => z)
                    .ToList();

                var overview = new ChatGroupHistory_UsageOverview
                {
                    MessageCount = filteredItems.Count,
                    PromptTokens = filteredItems.Sum(z => z.Usage.PromptTokens),
                    CompletionTokens = filteredItems.Sum(z => z.Usage.CompletionTokens),
                    TotalTokens = filteredItems.Sum(z => z.Usage.TotalTokens),
                    AverageResponseMilliseconds = responseMilliseconds.Count == 0 ? 0 : responseMilliseconds.Average(),
                    MinResponseMilliseconds = responseMilliseconds.Count == 0 ? 0 : responseMilliseconds.First(),
                    MaxResponseMilliseconds = responseMilliseconds.Count == 0 ? 0 : responseMilliseconds.Last(),
                    P95ResponseMilliseconds = CalculatePercentile(responseMilliseconds, 95)
                };

                var roundStats = filteredItems
                    .GroupBy(z => z.Usage.RoundIndex)
                    .OrderBy(z => z.Key)
                    .Select(g =>
                    {
                        var responses = g.Select(x => x.Usage.ResponseMilliseconds).Where(x => x > 0).ToList();
                        return new ChatGroupHistory_UsageRoundStat
                        {
                            RoundIndex = g.Key,
                            MessageCount = g.Count(),
                            PromptTokens = g.Sum(x => x.Usage.PromptTokens),
                            CompletionTokens = g.Sum(x => x.Usage.CompletionTokens),
                            TotalTokens = g.Sum(x => x.Usage.TotalTokens),
                            AverageResponseMilliseconds = responses.Count == 0 ? 0 : responses.Average()
                        };
                    })
                    .ToList();

                var agentStats = filteredItems
                    .Where(z => z.History.FromAgentTemplateId.HasValue)
                    .GroupBy(z => z.History.FromAgentTemplateId!.Value)
                    .Select(g =>
                    {
                        var responses = g.Select(x => x.Usage.ResponseMilliseconds).Where(x => x > 0).ToList();
                        return new ChatGroupHistory_UsageAgentStat
                        {
                            AgentTemplateId = g.Key,
                            AgentName = agentNameMap.TryGetValue(g.Key, out var name) ? name : $"Agent-{g.Key}",
                            MessageCount = g.Count(),
                            PromptTokens = g.Sum(x => x.Usage.PromptTokens),
                            CompletionTokens = g.Sum(x => x.Usage.CompletionTokens),
                            TotalTokens = g.Sum(x => x.Usage.TotalTokens),
                            AverageResponseMilliseconds = responses.Count == 0 ? 0 : responses.Average()
                        };
                    })
                    .OrderByDescending(z => z.TotalTokens)
                    .ThenBy(z => z.AgentTemplateId)
                    .ToList();

                var timelineStats = filteredItems
                    .GroupBy(z => new DateTime(
                        z.History.AddTime.Year,
                        z.History.AddTime.Month,
                        z.History.AddTime.Day,
                        z.History.AddTime.Hour,
                        0,
                        0))
                    .OrderBy(z => z.Key)
                    .Select(g =>
                    {
                        var responses = g.Select(x => x.Usage.ResponseMilliseconds).Where(x => x > 0).ToList();
                        return new ChatGroupHistory_UsageTimelineStat
                        {
                            BucketTime = g.Key,
                            MessageCount = g.Count(),
                            PromptTokens = g.Sum(x => x.Usage.PromptTokens),
                            CompletionTokens = g.Sum(x => x.Usage.CompletionTokens),
                            TotalTokens = g.Sum(x => x.Usage.TotalTokens),
                            AverageResponseMilliseconds = responses.Count == 0 ? 0 : responses.Average()
                        };
                    })
                    .ToList();

                return new ChatGroupHistory_GetUsageAnalyticsResponse
                {
                    Overview = overview,
                    RoundStats = roundStats,
                    AgentStats = agentStats,
                    TimelineStats = timelineStats
                };
            });
        }

        private static void PopulateUsageFields(ChatGroupHistoryDto historyDto)
        {
            if (historyDto == null || string.IsNullOrWhiteSpace(historyDto.AdminRemark))
            {
                return;
            }

            if (ChatUsageRemarkCodec.TryDecodeMessage(historyDto.AdminRemark, out var usage))
            {
                historyDto.PromptTokens = usage.PromptTokens;
                historyDto.CompletionTokens = usage.CompletionTokens;
                historyDto.TotalTokens = usage.TotalTokens;
                historyDto.ResponseMilliseconds = usage.ResponseMilliseconds;
                historyDto.RoundIndex = usage.RoundIndex;
                historyDto.ResponseId = usage.ResponseId;
            }
        }

        private static bool TryParseDateTime(string text, out DateTime dateTime)
        {
            dateTime = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-ddTHH:mm",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.fff",
                "o"
            };

            return DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime)
                   || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime);
        }

        private static int CalculatePercentile(List<int> sortedValues, int percentile)
        {
            if (sortedValues == null || sortedValues.Count == 0)
            {
                return 0;
            }

            if (percentile <= 0)
            {
                return sortedValues.First();
            }

            if (percentile >= 100)
            {
                return sortedValues.Last();
            }

            var index = (int)Math.Ceiling((percentile / 100.0) * sortedValues.Count) - 1;
            if (index < 0)
            {
                index = 0;
            }
            if (index >= sortedValues.Count)
            {
                index = sortedValues.Count - 1;
            }

            return sortedValues[index];
        }
    }
}
