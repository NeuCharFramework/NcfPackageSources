/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkspaceContracts.cs
    文件功能描述：工作区存储与访问策略契约
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.0 增加所有者、Agent 模板关联与访问策略

----------------------------------------------------------------*/

namespace Senparc.Xncf.Workspace.Abstractions;

public sealed record WorkspaceDescriptor(
    string Id,
    int OwnerId,
    string Name,
    string RootPath,
    DateTime CreatedUtc,
    DateTime LastAccessedUtc,
    string OwnerType = "User",
    int? AgentTemplateId = null,
    string StorageProvider = "FileSystem");

public sealed record WorkspaceEntry(
    string RelativePath,
    bool IsDirectory,
    long Length,
    DateTime LastWriteTimeUtc);

public sealed record WorkspaceCreateRequest(
    int OwnerId,
    string Name,
    string OwnerType = "User",
    int? AgentTemplateId = null);

public sealed record WorkspaceCommandRequest(
    string Command,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory = "");

public sealed record WorkspacePythonRequest(
    string ScriptPath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory = "");

public sealed record WorkspaceSearchRequest(
    string NameQuery = "",
    string ContentQuery = "",
    string RelativePath = "",
    bool CaseSensitive = false,
    int MaxResults = 100);

public sealed record WorkspaceSearchResult(
    string RelativePath,
    string MatchType,
    int? LineNumber,
    string Preview);

public sealed record WorkspacePatchRequest(
    string RelativePath,
    string OldText,
    string NewText,
    int ExpectedReplacements = 1,
    string ExpectedSha256 = "");

public sealed record WorkspacePatchResult(
    bool Applied,
    int ReplacementCount,
    string Message);

public sealed record WorkspaceCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);

public sealed record WorkspacePolicy(
    bool AllowRead = false,
    bool AllowWrite = false,
    bool AllowCommand = false,
    bool AllowPython = false,
    IReadOnlyList<string>? AllowedCommands = null,
    int MaxExecutionSeconds = 60,
    long MaxBytes = 50 * 1024 * 1024);

public interface IWorkspaceStorage
{
    Task<WorkspaceDescriptor> CreateAsync(
        WorkspaceCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkspaceDescriptor?> GetAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceDescriptor>> ListWorkspacesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
        string workspaceId,
        string relativePath = "",
        CancellationToken cancellationToken = default);

    Task<string> ResolveFilePathAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default);

    Task<string> ReadFileAsync(
        string workspaceId,
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default);

    Task WriteFileAsync(
        string workspaceId,
        string relativePath,
        string content,
        CancellationToken cancellationToken = default);

    Task CreateDirectoryAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
        string workspaceId,
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkspacePatchResult> ApplyPatchAsync(
        string workspaceId,
        WorkspacePatchRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkspaceCommandResult> ExecuteCommandAsync(
        string workspaceId,
        WorkspaceCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkspaceCommandResult> ExecutePythonAsync(
        string workspaceId,
        WorkspacePythonRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
