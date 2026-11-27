/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseChatToolAppService.cs
    File description: Provides KnowledgeBase chat-tool integration and parameter options.

    Created by: Senparc - 20261125

    Modified by: Senparc - 20261125
    Change description: v0.8.4 Added chat-tool parameter option integration.

    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

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
    /// <summary>
    /// Searches indexed knowledge bases by name or inline content and returns a bounded
    /// machine-readable list for AdminChat selection.
    /// </summary>
    /// <param name="request">The optional knowledge-base search query.</param>
    /// <returns>A function response containing matching indexed knowledge bases.</returns>
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

    /// <summary>
    /// Recalls passages from one indexed knowledge base for the supplied question.
    /// Validation failures are returned as function text so the chat workflow can recover.
    /// </summary>
    /// <param name="request">The knowledge-base identifier, query text, and requested result count.</param>
    /// <returns>A function response containing ranked recall evidence.</returns>
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
