/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupHistoryAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20241017

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.Application.Dtos;
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

        /// <summary>
        /// Initializes the chat-history application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="chatGroupHistoryService">The service used to query stored chat histories.</param>
        public ChatGroupHistoryAppService(IServiceProvider serviceProvider, ChatGroupHistoryService chatGroupHistoryService) : base(serviceProvider)
        {
            this._chatGroupHistoryService = chatGroupHistoryService;
        }

        /// <summary>
        /// Loads chat-history records after a caller's continuation identifier and enriches
        /// each record with decoded message and token-usage fields.
        /// </summary>
        /// <param name="chatTaskId">The chat-task identifier.</param>
        /// <param name="nextHistoryId">The last history identifier already returned to the caller.</param>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageSize">The maximum number of histories to return.</param>
        /// <returns>A response containing the ordered history DTO page.</returns>
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
        /// <summary>
        /// Aggregates token and response-time usage for one chat task, with optional
        /// Agent and date filters, round statistics, Agent statistics, and hourly timeline data.
        /// </summary>
        /// <param name="chatTaskId">The chat-task identifier to analyze.</param>
        /// <param name="agentTemplateId">Optional Agent template filter.</param>
        /// <param name="startTime">Optional inclusive start time.</param>
        /// <param name="endTime">Optional inclusive end time.</param>
        /// <returns>A response containing overview, round, Agent, and timeline analytics.</returns>
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

        /// <summary>
        /// Decodes usage metadata from a history remark and copies it into the DTO fields.
        /// </summary>
        /// <param name="historyDto">The history DTO to enrich.</param>
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

        /// <summary>
        /// Parses supported local and ISO date-time formats used by analytics filters.
        /// </summary>
        /// <param name="text">The date-time filter text.</param>
        /// <param name="dateTime">Receives the parsed date-time value.</param>
        /// <returns><see langword="true"/> when the text can be parsed.</returns>
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

        /// <summary>
        /// Selects a bounded percentile value from an already sorted integer list.
        /// </summary>
        /// <param name="sortedValues">The ascending sorted values.</param>
        /// <param name="percentile">The requested percentile from 0 through 100.</param>
        /// <returns>The selected percentile value, or zero for an empty list.</returns>
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
