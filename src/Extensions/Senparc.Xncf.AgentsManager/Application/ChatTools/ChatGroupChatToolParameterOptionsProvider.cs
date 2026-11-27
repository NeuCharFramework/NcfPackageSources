/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: ChatGroupChatToolParameterOptionsProvider.cs
    File description: Provides Agent group chat-tool parameter options.

    Created by: Senparc - 20261125

    Modified by: Senparc - 20261125
    Change description: v0.19.4 Added chat-tool parameter option integration.

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.AgentsManager.Domain.Services;

namespace Senparc.Xncf.AgentsManager.Application.ChatTools;

/// <summary>
/// Provides enabled ChatGroup options for Agent-manager chat-tool parameter selectors.
/// </summary>
public sealed class ChatGroupChatToolParameterOptionsProvider(ChatGroupService chatGroupService)
    : IChatToolParameterOptionsProvider
{
    /// <summary>
    /// Returns enabled ChatGroup options filtered by query text and bounded by the requested limit.
    /// </summary>
    /// <param name="request">The query and maximum option count.</param>
    /// <param name="cancellationToken">The cancellation token for option loading.</param>
    /// <returns>ChatGroup options suitable for a chat-tool selector.</returns>
    public async Task<IReadOnlyCollection<ChatToolParameterOption>> GetOptionsAsync(
        ChatToolParameterOptionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = request.Query?.Trim();
        var groups = await chatGroupService.GetObjectListAsync(
            0,
            Math.Clamp(request.Limit, 1, 100),
            group => group.Enable
                     && (string.IsNullOrWhiteSpace(query)
                         || group.Name.Contains(query)
                         || (group.Description != null && group.Description.Contains(query))
                         || group.Id.ToString().Contains(query)),
            group => group.Id,
            Senparc.Ncf.Core.Enums.OrderingType.Descending).ConfigureAwait(false);

        return groups.Select(group => new ChatToolParameterOption
        {
            Value = group.Id.ToString(),
            Label = group.Name,
            Description = string.IsNullOrWhiteSpace(group.Description)
                ? $"ChatGroup ID: {group.Id}"
                : $"#{group.Id} · {group.Description}",
            Keywords = $"{group.Id} {group.Name} {group.Description}"
        }).ToArray();
    }
}
