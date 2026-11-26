/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseChatToolParameterOptionsProvider.cs
    File description: Provides KnowledgeBase chat-tool integration and parameter options.

    Created by: Senparc - 20261125

    Modified by: Senparc - 20261125
    Change description: v0.8.4 Added chat-tool parameter option integration.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.KnowledgeBase.Domain.Services;

namespace Senparc.Xncf.KnowledgeBase.Application.ChatTools;

public sealed class KnowledgeBaseChatToolParameterOptionsProvider(
    KnowledgeBaseService knowledgeBaseService) : IChatToolParameterOptionsProvider
{
    /// <summary>
    /// Lists indexed KnowledgeBase definitions matching the optional query and
    /// converts them into bounded chat-tool parameter options.
    /// </summary>
    /// <param name="request">The query text and requested option limit.</param>
    /// <param name="cancellationToken">Token reserved for cancellation-aware callers.</param>
    /// <returns>Options ordered by recent update time and then name.</returns>
    public async Task<IReadOnlyCollection<ChatToolParameterOption>> GetOptionsAsync(
        ChatToolParameterOptionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = request.Query?.Trim();
        var knowledgeBases = await knowledgeBaseService.GetFullListAsync(
            knowledgeBase => knowledgeBase.EmbeddedTime.HasValue
                             && (string.IsNullOrWhiteSpace(query)
                                 || knowledgeBase.Id.ToString().Contains(query)
                                 || knowledgeBase.Name.Contains(query)
                                 || (knowledgeBase.Content != null && knowledgeBase.Content.Contains(query))))
            .ConfigureAwait(false);

        return knowledgeBases
            .OrderByDescending(knowledgeBase => knowledgeBase.LastUpdateTime)
            .ThenBy(knowledgeBase => knowledgeBase.Name)
            .Take(Math.Clamp(request.Limit, 1, 100))
            .Select(knowledgeBase => new ChatToolParameterOption
            {
                Value = knowledgeBase.Id.ToString(),
                Label = knowledgeBase.Name,
                Description = $"#{knowledgeBase.Id} · {knowledgeBase.EmbeddedTime:yyyy-MM-dd HH:mm}",
                Keywords = $"{knowledgeBase.Id} {knowledgeBase.Name} {knowledgeBase.Content}"
            })
            .ToArray();
    }
}
