/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkspaceContracts.cs
    文件功能描述：工作区存储与访问策略契约
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.0 增加所有者、Agent 模板关联与访问策略

    修改标识：Senparc - 20261102
    修改描述：增加 Workspace 服务器端流式文件上传契约

    修改标识：Senparc - 20261103
    修改描述：v0.2.0 add streamed large-file Workspace uploads with tenant-aware ownership checks

    修改标识：Senparc - 20261109
    修改描述：增加工作区用量统计与清空内容契约

    修改标识：Senparc - 20261111
    修改描述：搜索请求可改为按相对路径返回文件和目录候选

    修改标识：Senparc - 20261111
    修改描述：搜索支持内容正则表达式和相对路径 glob

    修改标识：Senparc - 20261112
    修改描述：v0.3.0 支持路径 glob、内容正则与更清晰的搜索契约错误

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

public sealed record WorkspaceReference(
    string WorkspaceId,
    string ReferenceType,
    string ReferenceId,
    string Description);

public interface IWorkspaceReferenceProvider
{
    Task<IReadOnlyList<WorkspaceReference>> GetReferencesAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}

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

public sealed record WorkspaceStreamWriteRequest(
    string RelativePath,
    bool Create = true,
    bool Overwrite = true,
    string ExpectedETag = "",
    long? ContentLength = null);

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

/// <summary>
/// 工作区内容用量。文件数和字节数都不包含根目录上的 .workspace.json 清单，
/// 也不跟随符号链接。
/// </summary>
public sealed record WorkspaceUsageStatistics(
    int FileCount,
    int DirectoryCount,
    long UsedBytes,
    long QuotaBytes);

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
    int MaxResults = 100,
    bool SuggestPaths = false,
    string PathGlob = "",
    bool ContentRegex = false);

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

    Task<WorkspaceTextWindow> ReadTextWindowAsync(
        string workspaceId,
        WorkspaceTextWindowRequest request,
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

    Task<WorkspaceWriteFileResult> WriteFileStreamAsync(
        string workspaceId,
        WorkspaceStreamWriteRequest request,
        Stream content,
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

    Task<WorkspaceUsageStatistics> GetUsageAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除工作区里的用户内容，并重建新建时的 input、output、logs、.workspace 目录。
    /// 保留工作区标识、清单和元数据。
    /// </summary>
    Task ClearContentAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
