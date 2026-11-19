/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SandboxSessionStatus.cs
    File description: SandboxSessionStatus related implementation

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.3.2 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Abstractions;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public enum SandboxSessionStatus
{
    Creating = 0,
    Running = 1,
    Stopping = 2,
    Stopped = 3,
    Failed = 4,
    Expired = 5
}
