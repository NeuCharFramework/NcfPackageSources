/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HumanInTheLoopPolicy.cs
    File description: Related implementation.
    Change description: Updated module behavior and related support.

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>Defines the human-in-the-loop policy level.</summary>
public enum HumanInTheLoopLevel
{
    /// <summary>Allows tools to run automatically.</summary>
    Automatic = 0,

    /// <summary>Uses risk-based approval rules.</summary>
    RiskBased = 1,

    /// <summary>Requests approval before tool calls.</summary>
    ToolApproval = 2,

    /// <summary>Requires a human participant in the execution.</summary>
    HumanParticipant = 3
}

/// <summary>Defines how plugin and MCP tools may be invoked.</summary>
public enum ToolPermissionMode
{
    /// <summary>Uses the effective level's default permission.</summary>
    Inherit = 0,

    /// <summary>automaticexecute。</summary>
    Automatic = 1,

    /// <summary>Requires human approval before invocation.</summary>
    RequireApproval = 2,

    /// <summary>Denies invocation.</summary>
    Deny = 3
}

/// <summary>Contains the normalized policy applied to one Agent execution.</summary>
public sealed record EffectiveHumanInTheLoopPolicy(
    HumanInTheLoopLevel Level,
    ToolPermissionMode PluginTools,
    ToolPermissionMode McpTools,
    bool IncludeHumanParticipant);

/// <summary>Resolves legacy flags and explicit overrides into an effective policy.</summary>
public static class HumanInTheLoopPolicyResolver
{
    /// <summary>
    /// Resolves tool permissions and human-participant behavior from the supplied policy inputs.
    /// </summary>
    /// <param name="level">The requested human-in-the-loop level.</param>
    /// <param name="pluginTools">The plugin permission override.</param>
    /// <param name="mcpTools">The MCP permission override.</param>
    /// <param name="legacyRequireHumanApproval">Legacy flag requiring approval for all tools.</param>
    /// <param name="includeHumanParticipant">Whether a human participant should be included.</param>
    /// <returns>The normalized effective policy.</returns>
    public static EffectiveHumanInTheLoopPolicy Resolve(
        HumanInTheLoopLevel level,
        ToolPermissionMode pluginTools,
        ToolPermissionMode mcpTools,
        bool legacyRequireHumanApproval = false,
        bool includeHumanParticipant = false)
    {
        var normalizedLevel = Enum.IsDefined(level) ? level : HumanInTheLoopLevel.Automatic;

        var defaultPluginMode = normalizedLevel switch
        {
            HumanInTheLoopLevel.RiskBased => ToolPermissionMode.Automatic,
            HumanInTheLoopLevel.ToolApproval => ToolPermissionMode.RequireApproval,
            HumanInTheLoopLevel.HumanParticipant => ToolPermissionMode.RequireApproval,
            _ => ToolPermissionMode.Automatic
        };
        var defaultMcpMode = normalizedLevel switch
        {
            HumanInTheLoopLevel.RiskBased => ToolPermissionMode.RequireApproval,
            HumanInTheLoopLevel.ToolApproval => ToolPermissionMode.RequireApproval,
            HumanInTheLoopLevel.HumanParticipant => ToolPermissionMode.RequireApproval,
            _ => ToolPermissionMode.Automatic
        };

        if (legacyRequireHumanApproval)
        {
            defaultPluginMode = ToolPermissionMode.RequireApproval;
            defaultMcpMode = ToolPermissionMode.RequireApproval;
            if (normalizedLevel == HumanInTheLoopLevel.Automatic)
            {
                normalizedLevel = HumanInTheLoopLevel.ToolApproval;
            }
        }

        return new EffectiveHumanInTheLoopPolicy(
            normalizedLevel,
            ApplyOverride(defaultPluginMode, pluginTools),
            ApplyOverride(defaultMcpMode, mcpTools),
            includeHumanParticipant || normalizedLevel == HumanInTheLoopLevel.HumanParticipant);
    }

    /// <summary>
    /// Resolves a requested tool permission override against the effective default.
    /// Deny is authoritative, while existing approval requirements cannot be relaxed.
    /// </summary>
    /// <param name="defaultMode">The default permission mode.</param>
    /// <param name="requestedMode">The requested override.</param>
    /// <returns>The effective permission mode.</returns>
    private static ToolPermissionMode ApplyOverride(
        ToolPermissionMode defaultMode,
        ToolPermissionMode requestedMode)
    {
        if (requestedMode == ToolPermissionMode.Inherit)
        {
            return defaultMode;
        }

        if (requestedMode == ToolPermissionMode.Deny)
        {
            return ToolPermissionMode.Deny;
        }

        if (requestedMode == ToolPermissionMode.RequireApproval
            || defaultMode == ToolPermissionMode.RequireApproval)
        {
            return ToolPermissionMode.RequireApproval;
        }

        return requestedMode;
    }
}

/// <summary>Provides stable identifiers for the built-in human participant.</summary>
public static class HumanParticipantConstants
{
    /// <summary>Gets the built-in human-participant prompt code.</summary>
    public const string PromptCode = "system:human-participant";
    /// <summary>Gets the participant display name.</summary>
    public const string Name = "Human";
    /// <summary>Gets the stable participant key.</summary>
    public const string ParticipantKey = "human:default";
    /// <summary>Gets the participant kind identifier.</summary>
    public const string ParticipantKind = "Human";

    /// <summary>Determines whether a prompt code identifies the built-in human participant.</summary>
    /// <param name="promptCode">The prompt code to inspect.</param>
    /// <returns><see langword="true"/> when the code matches.</returns>
    public static bool IsHuman(string promptCode)
        => string.Equals(promptCode?.Trim(), PromptCode, StringComparison.Ordinal);
}
