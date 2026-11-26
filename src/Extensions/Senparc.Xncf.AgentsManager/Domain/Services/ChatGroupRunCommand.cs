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

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Carries the group, model, prompt, hook, cancellation, and human-in-the-loop policy
/// used to start one Agent-group execution.
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

    /// <summary>Gets or sets the AI model identifier.</summary>
    public int AiModelId { get; set; }

    /// <summary>Gets or sets the command or prompt submitted to the group.</summary>
    public string PromptCommand { get; set; }

    /// <summary>
    /// description
    /// </summary>
    public string Description { get; set; }

    /// <summary>Gets or sets whether personality behavior is enabled.</summary>
    public bool Personality { get; set; }

    /// <summary>Gets or sets the hook platform used for output delivery.</summary>
    public HookPlatform HookPlatform { get; set; }

    /// <summary>Gets or sets the hook platform parameter.</summary>
    public string HookParameter { get; set; }

    /// <summary>Gets or sets the maximum chat round count.</summary>
    public int ChatMaxRound { get; set; } = ChatGroupService.DefaultChatMaxRound;

    /// <summary>Gets or sets the correlation identifier shared by the run and streamed events.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets whether tool calls require human approval.</summary>
    public bool RequireHumanApproval { get; set; }

    /// <summary>Gets or sets the human-in-the-loop level.</summary>
    public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; } = HumanInTheLoopLevel.Automatic;

    /// <summary>Gets or sets the plugin tool permission mode.</summary>
    public ToolPermissionMode PluginToolPermission { get; set; } = ToolPermissionMode.Inherit;

    /// <summary>Gets or sets the MCP tool permission mode.</summary>
    public ToolPermissionMode McpToolPermission { get; set; } = ToolPermissionMode.Inherit;

    /// <summary>Gets or sets whether a human participant should be included.</summary>
    public bool IncludeHumanParticipant { get; set; }

    /// <summary>Gets or sets whether connector invocation is limited to selected instances.</summary>
    public bool LimitConnectors { get; set; }

    /// <summary>Gets or sets connector instance identifiers allowed for this run.</summary>
    public List<string> ConnectorInstanceIds { get; set; }

    /// <summary>Gets or sets the user receiving human-interaction notifications.</summary>
    public string HumanRecipientUserId { get; set; }

    /// <summary>Gets or sets the cancellation token for the execution.</summary>
    public CancellationToken CancellationToken { get; set; }
}
