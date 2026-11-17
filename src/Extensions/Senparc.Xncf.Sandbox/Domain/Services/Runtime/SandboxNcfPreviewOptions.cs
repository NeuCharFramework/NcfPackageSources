/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxNcfPreviewOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20260814

    Modified by: Senparc - 20260815
    Change description: v0.2.0 add the NCF preview sandbox workload

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// Configuration section: SenparcXncfSandbox:NcfPreview.
/// <para>
/// The preview image must be supplied through Images:Overrides:ncf-preview and pinned by digest.
/// The default is disabled, and dependency restore egress is disabled even when the workload is
/// enabled. An organisation that enables restore must point at a restricted package-mirror network.
/// </para>
/// </summary>
public sealed class SandboxNcfPreviewOptions
{
    public const string SectionName = "SenparcXncfSandbox:NcfPreview";

    public bool Enabled { get; set; }
    public bool AllowDependencyRestoreNetwork { get; set; }
    public string? RestoreNetworkName { get; set; }
    public int StartupTimeoutSeconds { get; set; } = 180;
}
