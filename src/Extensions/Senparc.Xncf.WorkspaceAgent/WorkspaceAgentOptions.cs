/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceAgentOptions.cs
    File description: Workspace Agent file and execution limits

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.1 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.2.4 Improved Workspace Agent runtime diagnostics and packaging

----------------------------------------------------------------*/

namespace Senparc.Xncf.WorkspaceAgent;

public sealed record WorkspaceAgentOptions(
    string RootPath,
    long MaxWorkspaceBytes = 2L * 1024 * 1024 * 1024,
    int MaxEntriesPerList = 5000,
    int MaxFileReadBytes = 2 * 1024 * 1024,
    int MaxFileWriteBytes = 10 * 1024 * 1024,
    int MaxSearchFileBytes = 4 * 1024 * 1024,
    int MaxSearchFiles = 10_000,
    int MaxSearchResults = 200,
    int CommandTimeoutSeconds = 60,
    int MaxCommandOutputBytes = 2 * 1024 * 1024,
    bool ExecutionEnabled = false,
    string IsolationLevel = "NativeFileAccess",
    IReadOnlyList<string>? AllowedCommands = null);
