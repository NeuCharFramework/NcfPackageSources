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

public sealed record WorkspaceFileMetadata(
    string RelativePath,
    bool IsDirectory,
    long Length,
    DateTime CreationTimeUtc,
    DateTime LastWriteTimeUtc,
    string ETag);

public sealed record WorkspaceFileContent(
    WorkspaceFileMetadata Metadata,
    byte[] Content);

public sealed record WorkspaceWriteFileRequest(
    string RelativePath,
    byte[] Content,
    bool Create = true,
    bool Overwrite = true,
    string ExpectedETag = "");

public sealed record WorkspaceWriteFileResult(
    WorkspaceFileMetadata Metadata,
    bool Created);

public sealed record WorkspaceChangeEvent(
    long Sequence,
    string WorkspaceId,
    string RelativePath,
    string ChangeType,
    DateTimeOffset Time,
    string PreviousRelativePath = "",
    string ETag = "");

public sealed record WorkspaceDeleteEntryRequest(
    string RelativePath,
    bool Recursive = false,
    string ExpectedETag = "");

public sealed record WorkspaceMoveEntryRequest(
    string SourceRelativePath,
    string TargetRelativePath,
    bool Overwrite = false,
    string ExpectedETag = "");

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

public sealed record WorkspaceAccessContext(
    string WorkspaceId,
    WorkspacePolicy Policy,
    string ClientType,
    string ActorId,
    string SessionId = "");

public interface IWorkspaceClient : IAsyncDisposable
{
    string WorkspaceId { get; }

    WorkspacePolicy Policy { get; }

    Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
        string relativePath = "",
        CancellationToken cancellationToken = default);

    Task<WorkspaceFileMetadata> GetFileInfoAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    Task<WorkspaceFileContent> ReadFileAsync(
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default);

    Task<WorkspaceWriteFileResult> WriteFileAsync(
        WorkspaceWriteFileRequest request,
        CancellationToken cancellationToken = default);

    Task CreateDirectoryAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    Task DeleteEntryAsync(
        WorkspaceDeleteEntryRequest request,
        CancellationToken cancellationToken = default);

    Task MoveEntryAsync(
        WorkspaceMoveEntryRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkspacePatchResult> ApplyPatchAsync(
        WorkspacePatchRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkspaceCommandResult> ExecuteCommandAsync(
        WorkspaceCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkspaceCommandResult> ExecutePythonAsync(
        WorkspacePythonRequest request,
        CancellationToken cancellationToken = default);
}

public interface IWorkspaceClientFactory
{
    IWorkspaceClient Create(WorkspaceAccessContext context);
}

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

    Task<WorkspaceFileMetadata> GetFileInfoAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default);

    Task<WorkspaceFileContent> ReadFileBytesAsync(
        string workspaceId,
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default);

    Task WriteFileAsync(
        string workspaceId,
        string relativePath,
        string content,
        CancellationToken cancellationToken = default);

    Task<WorkspaceWriteFileResult> WriteFileBytesAsync(
        string workspaceId,
        WorkspaceWriteFileRequest request,
        CancellationToken cancellationToken = default);

    Task CreateDirectoryAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default);

    Task DeleteEntryAsync(
        string workspaceId,
        WorkspaceDeleteEntryRequest request,
        CancellationToken cancellationToken = default);

    Task MoveEntryAsync(
        string workspaceId,
        WorkspaceMoveEntryRequest request,
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
