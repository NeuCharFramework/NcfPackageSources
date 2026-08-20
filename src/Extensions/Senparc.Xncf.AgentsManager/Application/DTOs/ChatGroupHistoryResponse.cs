/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupHistoryResponse.cs
    文件功能描述：ChatGroupHistoryResponse 数据传输对象定义
    
    
    创建标识：Senparc - 20241017
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

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
