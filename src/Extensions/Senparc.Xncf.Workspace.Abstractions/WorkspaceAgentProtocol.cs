/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceAgentProtocol.cs
    文件功能描述：Workspace Agent 跨进程协议

    创建标识：Senparc - 20261024

    修改标识：Senparc - 20261103
    修改描述：v0.2.0 implement connection manager and workspace file system provider

    修改标识：Senparc - 20261111
    修改描述：v0.2.0 完善 Workspace Agent、文件存储与模块注册兼容

----------------------------------------------------------------*/

using System.Text.Json;

namespace Senparc.Xncf.Workspace.Abstractions;

public static class WorkspaceAgentOperations
{
    public const string Ping = "ping";
    public const string List = "list";
    public const string Stat = "stat";
    public const string Read = "read";
    public const string ReadTextWindow = "read-text-window";
    public const string Write = "write";
    public const string CreateDirectory = "mkdir";
    public const string Delete = "delete";
    public const string Move = "move";
    public const string Search = "search";
    public const string Patch = "patch";
    public const string ExecuteCommand = "execute-command";
    public const string ExecutePython = "execute-python";
    public const string Shutdown = "shutdown";
}

public sealed record WorkspaceAgentRequest(
    string Id,
    string Operation,
    JsonElement Payload);

public sealed record WorkspaceAgentResponse(
    string Id,
    bool Succeeded,
    JsonElement? Result = null,
    WorkspaceAgentError? Error = null);

public sealed record WorkspaceAgentError(
    string Code,
    string Message);

public sealed record WorkspaceAgentListRequest(string RelativePath = "");

public sealed record WorkspaceAgentPathRequest(string RelativePath);

public sealed record WorkspaceAgentReadRequest(string RelativePath, int MaxBytes);

public sealed record WorkspaceTextWindowRequest(
    string RelativePath,
    int StartLine = 1,
    int LineCount = 800,
    int MaxBytes = 384 * 1024);

public sealed record WorkspaceTextWindow(
    string RelativePath,
    long FileLength,
    int StartLine,
    int LineCount,
    bool HasMore,
    bool Truncated,
    string Content,
    string ETag);

public sealed record WorkspaceAgentRuntimeInfo(
    int ProcessId,
    string OperatingSystem,
    string RootPath,
    bool ExecutionEnabled,
    string IsolationLevel);

public sealed class WorkspaceConflictException : InvalidOperationException
{
    public WorkspaceConflictException(string message)
        : base(message)
    {
    }
}

public sealed class WorkspaceLimitException : InvalidOperationException
{
    public WorkspaceLimitException(string message)
        : base(message)
    {
    }
}
