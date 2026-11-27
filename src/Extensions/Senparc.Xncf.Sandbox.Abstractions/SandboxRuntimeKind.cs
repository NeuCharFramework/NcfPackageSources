/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SandboxRuntimeKind.cs
    File description: SandboxRuntimeKind related implementation

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.3.2 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.3.2.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Abstractions;

/// <summary>
/// Identifies the runtime provider used to host a Sandbox session.
/// </summary>
public enum SandboxRuntimeKind
{
    Docker = 0,
    Wasm = 1
}
