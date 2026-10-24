/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceAgentOptions.cs
    文件功能描述：Workspace Agent 文件与执行限制

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

namespace Senparc.Xncf.Workspace.Agent;

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
