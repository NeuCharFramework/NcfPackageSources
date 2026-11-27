/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceAgentProtocol.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.0 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261111
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.1 Localized workspace management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.3.1.2 Improve source documentation, headers, and module metadata consistency.

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
    public const string Copy = "copy";
    public const string Search = "search";
    public const string Patch = "patch";
    public const string ExecuteCommand = "execute-command";
    public const string ExecutePython = "execute-python";
    public const string Shutdown = "shutdown";
}

/// <summary>
/// Describes one NDJSON request sent to a Workspace Agent host.
/// </summary>
/// <param name="Id">The caller-defined correlation identifier.</param>
/// <param name="Operation">The operation name dispatched by the host.</param>
/// <param name="Payload">The operation-specific JSON payload.</param>
public sealed record WorkspaceAgentRequest(
    string Id,
    string Operation,
    JsonElement Payload);

/// <summary>
/// Represents one successful or failed NDJSON response from a Workspace Agent host.
/// </summary>
/// <param name="Id">The request correlation identifier.</param>
/// <param name="Succeeded">Whether the operation completed successfully.</param>
/// <param name="Result">The operation-specific result payload.</param>
/// <param name="Error">The structured error payload when the operation fails.</param>
public sealed record WorkspaceAgentResponse(
    string Id,
    bool Succeeded,
    JsonElement? Result = null,
    WorkspaceAgentError? Error = null);

/// <summary>
/// Carries a stable error code and diagnostic message for a failed Workspace Agent operation.
/// </summary>
/// <param name="Code">The machine-readable error category.</param>
/// <param name="Message">The diagnostic message safe for the protocol caller.</param>
public sealed record WorkspaceAgentError(
    string Code,
    string Message);

/// <summary>
/// Requests a directory listing below the supplied relative path.
/// </summary>
/// <param name="RelativePath">The workspace-relative directory path.</param>
public sealed record WorkspaceAgentListRequest(string RelativePath = "");

/// <summary>
/// Requests metadata or mutation for one workspace-relative path.
/// </summary>
/// <param name="RelativePath">The workspace-relative file or directory path.</param>
public sealed record WorkspaceAgentPathRequest(string RelativePath);

/// <summary>
/// Requests bounded binary content for one workspace-relative file.
/// </summary>
/// <param name="RelativePath">The workspace-relative file path.</param>
/// <param name="MaxBytes">The caller-requested maximum number of bytes.</param>
public sealed record WorkspaceAgentReadRequest(string RelativePath, int MaxBytes);

/// <summary>
/// Requests a bounded text window from a workspace file.
/// </summary>
/// <param name="RelativePath">The workspace-relative file path.</param>
/// <param name="StartLine">The one-based first line to return.</param>
/// <param name="LineCount">The maximum number of lines to return.</param>
/// <param name="MaxBytes">The maximum UTF-8 byte count for the returned text.</param>
public sealed record WorkspaceTextWindowRequest(
    string RelativePath,
    int StartLine = 1,
    int LineCount = 800,
    int MaxBytes = 384 * 1024);

/// <summary>
/// Contains a bounded text window and metadata describing whether more content exists.
/// </summary>
/// <param name="RelativePath">The workspace-relative source path.</param>
/// <param name="FileLength">The source file length in bytes.</param>
/// <param name="StartLine">The one-based line requested by the caller.</param>
/// <param name="LineCount">The number of lines returned.</param>
/// <param name="HasMore">Whether more source content exists after the returned window.</param>
/// <param name="Truncated">Whether at least one returned line exceeded the byte limit.</param>
/// <param name="Content">The UTF-8 text content of the window.</param>
/// <param name="ETag">The metadata tag used for optimistic concurrency checks.</param>
public sealed record WorkspaceTextWindow(
    string RelativePath,
    long FileLength,
    int StartLine,
    int LineCount,
    bool HasMore,
    bool Truncated,
    string Content,
    string ETag);

/// <summary>
/// Reports the runtime isolation and capability information of a Workspace Agent host.
/// </summary>
/// <param name="RootPath">The normalized workspace root.</param>
/// <param name="IsolationLevel">The configured isolation label.</param>
/// <param name="ExecutionEnabled">Whether process execution is enabled.</param>
/// <param name="AllowedCommands">The command names accepted by the host policy.</param>
public sealed record WorkspaceAgentRuntimeInfo(
    int ProcessId,
    string OperatingSystem,
    string RootPath,
    bool ExecutionEnabled,
    string IsolationLevel);

public sealed class WorkspaceConflictException : InvalidOperationException
{
    /// <summary>
    /// Initializes a workspace optimistic-concurrency exception.
    /// </summary>
    /// <param name="message">The conflict description.</param>
    public WorkspaceConflictException(string message)
        : base(message)
    {
    }
}

public sealed class WorkspaceLimitException : InvalidOperationException
{
    /// <summary>
    /// Initializes a workspace quota or operation-limit exception.
    /// </summary>
    /// <param name="message">The limit description.</param>
    public WorkspaceLimitException(string message)
        : base(message)
    {
    }
}
