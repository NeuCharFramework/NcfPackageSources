/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceAgentOptions.cs
    File description: Workspace Agent file and execution limits

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.1 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.2.4 Improved Workspace Agent runtime diagnostics and packaging

    Modified by: Senparc - 20261127
    Change description: v0.2.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Xncf.WorkspaceAgent;

/// <summary>
/// Defines the workspace root, resource quotas, search limits, and optional process-execution policy
/// used by <see cref="WorkspaceFileSystemEngine"/>.
/// </summary>
/// <param name="RootPath">The absolute or relative directory exposed as the workspace root.</param>
/// <param name="MaxWorkspaceBytes">The maximum aggregate size of workspace files accepted by write operations.</param>
/// <param name="MaxEntriesPerList">The maximum number of entries returned by one directory listing.</param>
/// <param name="MaxFileReadBytes">The maximum number of bytes read by a complete-file operation.</param>
/// <param name="MaxFileWriteBytes">The maximum number of bytes accepted by one file-write operation.</param>
/// <param name="MaxSearchFileBytes">The largest file inspected for content-search matches.</param>
/// <param name="MaxSearchFiles">The maximum number of files inspected during one search.</param>
/// <param name="MaxSearchResults">The maximum number of results returned by one search.</param>
/// <param name="CommandTimeoutSeconds">The maximum execution duration for an allowed process.</param>
/// <param name="MaxCommandOutputBytes">The maximum combined standard-output and standard-error bytes retained from a process.</param>
/// <param name="ExecutionEnabled">Whether process execution is permitted at all.</param>
/// <param name="IsolationLevel">The deployment isolation label reported by the host.</param>
/// <param name="AllowedCommands">The executable names permitted when process execution is enabled.</param>
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
