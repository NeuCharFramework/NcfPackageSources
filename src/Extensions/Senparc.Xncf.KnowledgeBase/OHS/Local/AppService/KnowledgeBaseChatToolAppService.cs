/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseChatToolAppService.cs
    File description: Provides KnowledgeBase chat-tool integration and parameter options.

    Created by: Senparc - 20261125

    Modified by: Senparc - 20261125
    Change description: v0.8.4 Added chat-tool parameter option integration.

----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.KnowledgeBase.Domain.Services;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService;

[ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class KnowledgeBaseChatToolAppService(
    IServiceProvider serviceProvider,
    KnowledgeBaseService knowledgeBaseService) : AppServiceBase(serviceProvider)
{
    [FunctionRender(
        typeof(KnowledgeBaseResource),
        "AdminChat.Function.SearchKnowledgeBases",
        "AdminChat.Function.SearchKnowledgeBasesDescription",
        typeof(Register))]
    public Task<StringAppResponse> SearchKnowledgeBases(KnowledgeBaseChatSearchRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            var query = request?.Query?.Trim() ?? string.Empty;
            var knowledgeBases = await knowledgeBaseService.GetFullListAsync(knowledgeBase =>
                knowledgeBase.EmbeddedTime.HasValue
                && (string.IsNullOrWhiteSpace(query)
                    || knowledgeBase.Name.Contains(query)
                    || (knowledgeBase.Content != null && knowledgeBase.Content.Contains(query))))
                .ConfigureAwait(false);
            var items = knowledgeBases
                .OrderByDescending(knowledgeBase => knowledgeBase.LastUpdateTime)
                .Take(20)
                .Select(knowledgeBase => new
                {
                    knowledgeBase.Id,
                    knowledgeBase.Name,
                    knowledgeBase.EmbeddedTime
                })
                .ToArray();
            logger.Append(JsonSerializer.Serialize(new { Query = query, Items = items }));
            return logger.GetLogs();
        });
    }

    [FunctionRender(
        typeof(KnowledgeBaseResource),
        "AdminChat.Function.RecallKnowledgeBase",
        "AdminChat.Function.RecallKnowledgeBaseDescription",
        typeof(Register))]
    public Task<StringAppResponse> RecallKnowledgeBase(KnowledgeBaseChatRecallRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            if (request?.KnowledgeBaseId <= 0)
            {
                return "KnowledgeBaseId must be greater than 0.";
            }
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return "Query is required.";
            }

            var knowledgeBase = await knowledgeBaseService.GetObjectAsync(item => item.Id == request.KnowledgeBaseId)
                .ConfigureAwait(false);
            if (knowledgeBase == null)
            {
                return $"Knowledge base #{request.KnowledgeBaseId} was not found.";
            }

            var results = await knowledgeBaseService.RecallTestAsync(
                    request.KnowledgeBaseId,
                    request.Query.Trim(),
                    Math.Clamp(request.TopK <= 0 ? 5 : request.TopK, 1, 20))
                .ConfigureAwait(false);
            logger.Append(JsonSerializer.Serialize(new
            {
                KnowledgeBaseId = knowledgeBase.Id,
                KnowledgeBaseName = knowledgeBase.Name,
                Query = request.Query.Trim(),
                Items = results.Select(item => new
                {
                    item.Rank,
                    item.Score,
                    item.Content,
                    item.SourceName,
                    item.SourceLink
                })
            }));
            return logger.GetLogs();
        });
    }
}

public sealed class KnowledgeBaseChatSearchRequest
{
    public string Query { get; set; }
}

public sealed class KnowledgeBaseChatRecallRequest
{
    public int KnowledgeBaseId { get; set; }
    public string Query { get; set; }
    public int TopK { get; set; } = 5;
}
