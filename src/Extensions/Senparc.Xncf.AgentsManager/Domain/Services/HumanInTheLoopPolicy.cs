/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: HumanInTheLoopPolicy.cs
    File description: Related implementation.

    Created by: Senparc - 20260815
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public enum HumanInTheLoopLevel
{
    ///Legacy comment translated to English.
    Automatic = 0,

    ///Legacy comment translated to English.
    RiskBased = 1,

    ///Legacy comment translated to English.
    ToolApproval = 2,

    ///Legacy comment translated to English.
    HumanParticipant = 3
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public enum ToolPermissionMode
{
    ///Legacy comment translated to English.
    Inherit = 0,

    /// <summary>automaticexecute。</summary>
    Automatic = 1,

    ///Legacy comment translated to English.
    RequireApproval = 2,

    ///Legacy comment translated to English.
    Deny = 3
}

public sealed record EffectiveHumanInTheLoopPolicy(
    HumanInTheLoopLevel Level,
    ToolPermissionMode PluginTools,
    ToolPermissionMode McpTools,
    bool IncludeHumanParticipant);

public static class HumanInTheLoopPolicyResolver
{
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

        //Legacy comment translated to English.
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

    private static ToolPermissionMode ApplyOverride(
        ToolPermissionMode defaultMode,
        ToolPermissionMode requestedMode)
    {
        if (requestedMode == ToolPermissionMode.Inherit)
        {
            return defaultMode;
        }

        //Legacy comment translated to English.
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

/// <summary>
///Legacy comment translated to English.
/// </summary>
public static class HumanParticipantConstants
{
    public const string PromptCode = "system:human-participant";
    public const string Name = "Human";
    public const string ParticipantKey = "human:default";
    public const string ParticipantKind = "Human";

    public static bool IsHuman(string promptCode)
        => string.Equals(promptCode?.Trim(), PromptCode, StringComparison.Ordinal);
}
