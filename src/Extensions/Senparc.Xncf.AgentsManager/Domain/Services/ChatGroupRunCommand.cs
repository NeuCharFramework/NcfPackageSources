/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupRunCommand.cs
    File description: Related implementation.

    Created by: Senparc - 20260927
    Creation description: Related implementation.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261111
    Change description: v0.19.3 Updated module behavior and related support.

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class ChatGroupRunCommand
{
    /// <summary>
    /// task name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// ChatGroup ID
    /// </summary>
    public int ChatGroupId { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int AiModelId { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public string PromptCommand { get; set; }

    /// <summary>
    /// description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool Personality { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public HookPlatform HookPlatform { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public string HookParameter { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int ChatMaxRound { get; set; } = ChatGroupService.DefaultChatMaxRound;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public bool RequireHumanApproval { get; set; }

    ///Legacy comment translated to English.
    public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; } = HumanInTheLoopLevel.Automatic;

    ///Legacy comment translated to English.
    public ToolPermissionMode PluginToolPermission { get; set; } = ToolPermissionMode.Inherit;

    ///Legacy comment translated to English.
    public ToolPermissionMode McpToolPermission { get; set; } = ToolPermissionMode.Inherit;

    ///Legacy comment translated to English.
    public bool IncludeHumanParticipant { get; set; }

    ///Legacy comment translated to English.
    public bool LimitConnectors { get; set; }

    ///Legacy comment translated to English.
    public List<string> ConnectorInstanceIds { get; set; }

    ///Legacy comment translated to English.
    public string HumanRecipientUserId { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}
