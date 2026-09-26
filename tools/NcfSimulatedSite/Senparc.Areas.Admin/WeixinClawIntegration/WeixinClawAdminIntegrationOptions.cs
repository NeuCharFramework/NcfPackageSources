using System;

namespace Senparc.Areas.Admin.WeixinClawIntegration;

/// <summary>
/// Admin 与个人微信 Claw 的上层集成选项。
/// </summary>
public sealed class WeixinClawAdminIntegrationOptions
{
    public const string SectionName = "WeixinClawAdminIntegration";

    public bool Enabled { get; set; } = true;
    public int DefaultAdminUserId { get; set; }
    public int DefaultAccountId { get; set; }
    public string BootstrapCode { get; set; } = string.Empty;
    public string CommandPrefix { get; set; } = "/";
    public bool RequireCommandPrefix { get; set; } = true;
    public bool AllowPlainChat { get; set; } = true;
    public bool EnableNeuBell { get; set; } = true;
    public bool EnableWorkflow { get; set; } = true;
    public bool EnableHarness { get; set; }
    public int DefaultAiModelId { get; set; }
    public int MaxHarnessIterations { get; set; } = 16;
    public int MaxReplyLength { get; set; } = 1500;
    public int MaxInputLength { get; set; } = 8000;
    public TimeSpan HarnessTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
