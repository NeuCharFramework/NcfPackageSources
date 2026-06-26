using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class ChatGroupHistory_GetListResponse
    {
        public PagedList<ChatGroupHistoryDto> ChatGroupHistories { get; set; }
    }

    public class ChatGroupHistory_GetUsageAnalyticsResponse
    {
        public ChatGroupHistory_UsageOverview Overview { get; set; } = new();

        public List<ChatGroupHistory_UsageRoundStat> RoundStats { get; set; } = new();

        public List<ChatGroupHistory_UsageAgentStat> AgentStats { get; set; } = new();

        public List<ChatGroupHistory_UsageTimelineStat> TimelineStats { get; set; } = new();
    }

    public class ChatGroupHistory_UsageOverview
    {
        public int MessageCount { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public double AverageResponseMilliseconds { get; set; }
        public int MinResponseMilliseconds { get; set; }
        public int MaxResponseMilliseconds { get; set; }
        public int P95ResponseMilliseconds { get; set; }
    }

    public class ChatGroupHistory_UsageRoundStat
    {
        public int RoundIndex { get; set; }
        public int MessageCount { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public double AverageResponseMilliseconds { get; set; }
    }

    public class ChatGroupHistory_UsageAgentStat
    {
        public int AgentTemplateId { get; set; }
        public string AgentName { get; set; }
        public int MessageCount { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public double AverageResponseMilliseconds { get; set; }
    }

    public class ChatGroupHistory_UsageTimelineStat
    {
        public DateTime BucketTime { get; set; }
        public int MessageCount { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public double AverageResponseMilliseconds { get; set; }
    }
}
