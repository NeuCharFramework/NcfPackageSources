/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawAdminIntegrationOptions.cs
    File description: WeixinClawAdminIntegrationOptions.cs implementation and project behavior.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add WeixinClawBindingProfile management and UI enhancements

----------------------------------------------------------------*/

using System;

namespace Senparc.Areas.Admin.WeixinClawIntegration;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class WeixinClawAdminIntegrationOptions
{
    public const string SectionName = "WeixinClawAdminIntegration";

    public bool Enabled { get; set; } = true;
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
