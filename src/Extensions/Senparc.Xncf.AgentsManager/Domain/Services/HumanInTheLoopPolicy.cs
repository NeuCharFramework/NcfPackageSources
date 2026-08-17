/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：HumanInTheLoopPolicy.cs
    文件功能描述：AgentsManager 的 HIL 等级与工具权限策略

    创建标识：Senparc - 20260815
    修改描述：将工具审批、人类参与者和自动执行拆成可组合的策略

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// HIL 的主等级。工具权限仍可通过 <see cref="ToolPermissionMode"/> 单独收紧。
/// </summary>
public enum HumanInTheLoopLevel
{
    /// <summary>全自动运行，不等待人工。</summary>
    Automatic = 0,

    /// <summary>风险分层：插件工具自动，MCP 工具需要确认。</summary>
    RiskBased = 1,

    /// <summary>所有可执行工具调用前请求人工确认。</summary>
    ToolApproval = 2,

    /// <summary>包含工具确认，并允许 Group 中的 Human 参与者实际发言。</summary>
    HumanParticipant = 3
}

/// <summary>
/// 某一类工具的执行权限。只允许向更严格的方向覆盖 Group 默认策略。
/// </summary>
public enum ToolPermissionMode
{
    /// <summary>使用 HIL 等级计算出的默认值。</summary>
    Inherit = 0,

    /// <summary>自动执行。</summary>
    Automatic = 1,

    /// <summary>执行前请求人工确认。</summary>
    RequireApproval = 2,

    /// <summary>不向模型暴露此类工具。</summary>
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

        // 旧版 bool 的语义是“所有工具审批”，必须优先保留。
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

        // Deny 永远优先；RequireApproval 不能被低权限的 Automatic 覆盖。
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
/// Human 参与者使用现有 AgentTemplate 外键兼容存储，但通过固定标识与普通模型 Agent 分离。
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
