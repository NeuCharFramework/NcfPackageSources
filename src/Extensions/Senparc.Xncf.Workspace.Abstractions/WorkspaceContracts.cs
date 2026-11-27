/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20261015

    Modified by: Senparc - 20261021
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261102
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.2.0 add streamed large-file Workspace uploads with tenant-aware ownership checks

    Modified by: Senparc - 20261109
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261112
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.1 Localized workspace management and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.Workspace.Abstractions;

/// <summary>Describes a workspace's identity, owner, storage location, and access metadata.</summary>
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

/// <summary>Describes an external object associated with a workspace.</summary>
public sealed record WorkspaceReference(
    string WorkspaceId,
    string ReferenceType,
    string ReferenceId,
    string Description);

/// <summary>
/// Resolves external references associated with a workspace.
/// </summary>
public interface IWorkspaceReferenceProvider
{
    /// <summary>
    /// Retrieves references associated with the specified workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier whose references should be returned.</param>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns>A task that completes with the references known for the workspace.</returns>
    Task<IReadOnlyList<WorkspaceReference>> GetReferencesAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}

/// <summary>Describes one file-system entry returned by a workspace listing.</summary>
public sealed record WorkspaceEntry(
    string RelativePath,
    bool IsDirectory,
    long Length,
    DateTime LastWriteTimeUtc);

/// <summary>Describes file/directory metadata and the concurrency ETag.</summary>
public sealed record WorkspaceFileMetadata(
    string RelativePath,
    bool IsDirectory,
    long Length,
    DateTime CreationTimeUtc,
    DateTime LastWriteTimeUtc,
    string ETag);

/// <summary>Contains workspace file metadata and raw file bytes.</summary>
public sealed record WorkspaceFileContent(
    WorkspaceFileMetadata Metadata,
    byte[] Content);

/// <summary>Specifies byte content, path, creation, overwrite, and ETag rules for a write.</summary>
public sealed record WorkspaceWriteFileRequest(
    string RelativePath,
    byte[] Content,
    bool Create = true,
    bool Overwrite = true,
    string ExpectedETag = "");

/// <summary>Specifies path and concurrency rules for a streamed write.</summary>
public sealed record WorkspaceStreamWriteRequest(
    string RelativePath,
    bool Create = true,
    bool Overwrite = true,
    string ExpectedETag = "",
    long? ContentLength = null);

/// <summary>Reports the resulting file metadata and whether the write created a new file.</summary>
public sealed record WorkspaceWriteFileResult(
    WorkspaceFileMetadata Metadata,
    bool Created);

/// <summary>Describes one ordered workspace content change notification.</summary>
public sealed record WorkspaceChangeEvent(
    long Sequence,
    string WorkspaceId,
    string RelativePath,
    string ChangeType,
    DateTimeOffset Time,
    string PreviousRelativePath = "",
    string ETag = "");

/// <summary>Specifies a workspace entry deletion and its recursive/ETag rules.</summary>
public sealed record WorkspaceDeleteEntryRequest(
    string RelativePath,
    bool Recursive = false,
    string ExpectedETag = "");

/// <summary>Specifies a workspace entry move and its overwrite/ETag rules.</summary>
public sealed record WorkspaceMoveEntryRequest(
    string SourceRelativePath,
    string TargetRelativePath,
    bool Overwrite = false,
    string ExpectedETag = "");

/// <summary>Specifies a workspace entry copy and its overwrite/ETag rules.</summary>
public sealed record WorkspaceCopyEntryRequest(
    string SourceRelativePath,
    string TargetRelativePath,
    bool Overwrite = false,
    string ExpectedETag = "");

/// <summary>Contains owner metadata used to create a workspace.</summary>
public sealed record WorkspaceCreateRequest(
    int OwnerId,
    string Name,
    string OwnerType = "User",
    int? AgentTemplateId = null);

/// <summary>
/// Reports the file and directory usage of a workspace against its configured quota.
/// </summary>
public sealed record WorkspaceUsageStatistics(
    int FileCount,
    int DirectoryCount,
    long UsedBytes,
    long QuotaBytes);

/// <summary>Specifies an allowed workspace command and working directory.</summary>
public sealed record WorkspaceCommandRequest(
    string Command,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory = "");

/// <summary>Specifies an allowed Python script and execution arguments.</summary>
public sealed record WorkspacePythonRequest(
    string ScriptPath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory = "");

/// <summary>Contains workspace path/content search filters and result limits.</summary>
public sealed record WorkspaceSearchRequest(
    string NameQuery = "",
    string ContentQuery = "",
    string RelativePath = "",
    bool CaseSensitive = false,
    int MaxResults = 100,
    bool SuggestPaths = false,
    string PathGlob = "",
    bool ContentRegex = false);

/// <summary>Describes one path or content search match.</summary>
public sealed record WorkspaceSearchResult(
    string RelativePath,
    string MatchType,
    int? LineNumber,
    string Preview);

/// <summary>Specifies an exact-text patch and optional hash/replacement safeguards.</summary>
public sealed record WorkspacePatchRequest(
    string RelativePath,
    string OldText,
    string NewText,
    int ExpectedReplacements = 1,
    string ExpectedSha256 = "");

/// <summary>Reports whether a workspace patch was applied and how many replacements occurred.</summary>
public sealed record WorkspacePatchResult(
    bool Applied,
    int ReplacementCount,
    string Message);

/// <summary>Contains command exit code, bounded output, and timeout state.</summary>
public sealed record WorkspaceCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);

/// <summary>Defines policy permissions, command allow-list, execution timeout, and byte quota.</summary>
public sealed record WorkspacePolicy(
    bool AllowRead = false,
    bool AllowWrite = false,
    bool AllowCommand = false,
    bool AllowPython = false,
    IReadOnlyList<string>? AllowedCommands = null,
    int MaxExecutionSeconds = 60,
    long MaxBytes = 50 * 1024 * 1024);

/// <summary>Binds a workspace client to identity, session, and policy context.</summary>
public sealed record WorkspaceAccessContext(
    string WorkspaceId,
    WorkspacePolicy Policy,
    string ClientType,
    string ActorId,
    string SessionId = "");

/// <summary>
/// Provides policy-bound file-system and execution operations for one workspace.
/// Implementations must enforce the <see cref="WorkspaceAccessContext"/> policy before
/// performing reads, writes, searches, patches, or process execution.
/// </summary>
public interface IWorkspaceClient : IAsyncDisposable
{
    /// <summary>
    /// Gets the identifier of the workspace served by this client.
    /// </summary>
    string WorkspaceId { get; }

    /// <summary>
    /// Gets the access policy applied to all operations performed by this client.
    /// </summary>
    WorkspacePolicy Policy { get; }

    /// <summary>
    /// Lists non-reserved entries below a workspace-relative directory.
    /// </summary>
    /// <param name="relativePath">The workspace-relative directory path; an empty value refers to the workspace root.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>A task that completes with the visible entries ordered by the implementation.</returns>
    Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
        string relativePath = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metadata and the current entity tag for a workspace-relative file or directory.
    /// </summary>
    /// <param name="relativePath">The workspace-relative path to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel the metadata lookup.</param>
    /// <returns>A task that completes with the current metadata.</returns>
    Task<WorkspaceFileMetadata> GetFileInfoAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a file into memory, subject to the caller's byte limit and the implementation's workspace policy.
    /// </summary>
    /// <param name="relativePath">The workspace-relative file path.</param>
    /// <param name="maxBytes">The maximum number of bytes the caller is willing to receive.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>A task that completes with file metadata and the file bytes.</returns>
    Task<WorkspaceFileContent> ReadFileAsync(
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or replaces a file using the request's create, overwrite, and entity-tag rules.
    /// </summary>
    /// <param name="request">The write request containing the target path, content, and concurrency options.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    /// <returns>A task that completes with the resulting metadata and whether a new file was created.</returns>
    Task<WorkspaceWriteFileResult> WriteFileAsync(
        WorkspaceWriteFileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a directory below the workspace root.
    /// </summary>
    /// <param name="relativePath">The workspace-relative directory path to create.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CreateDirectoryAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file or directory after applying the request's recursive and concurrency rules.
    /// </summary>
    /// <param name="request">The delete request containing the target path and deletion policy.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task DeleteEntryAsync(
        WorkspaceDeleteEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a workspace entry to a new workspace-relative path.
    /// </summary>
    /// <param name="request">The move request containing source, target, overwrite, and concurrency options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task MoveEntryAsync(
        WorkspaceMoveEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a workspace entry to a new workspace-relative path.
    /// </summary>
    /// <param name="request">The copy source, target, overwrite, and concurrency options.</param>
    /// <param name="cancellationToken">Token used to cancel the copy.</param>
    Task CopyEntryAsync(
        WorkspaceCopyEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches workspace paths and, when requested, file contents using the supplied search constraints.
    /// </summary>
    /// <param name="request">The path, content, glob, regular-expression, and result-limit options.</param>
    /// <param name="cancellationToken">Token used to cancel traversal and content inspection.</param>
    /// <returns>A task that completes with matching workspace-relative results.</returns>
    Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an exact-text replacement after checking the optional content hash and expected match count.
    /// </summary>
    /// <param name="request">The patch text, target file, expected replacement count, and optional hash.</param>
    /// <param name="cancellationToken">Token used to cancel the read-check-write sequence.</param>
    /// <returns>A task that reports whether the patch was applied and how many matches were found.</returns>
    Task<WorkspacePatchResult> ApplyPatchAsync(
        WorkspacePatchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an explicitly allowed command inside the requested workspace directory.
    /// </summary>
    /// <param name="request">The command, arguments, working directory, and execution options.</param>
    /// <param name="cancellationToken">Token used to cancel the process operation.</param>
    /// <returns>A task that completes with exit code, bounded output, and timeout information.</returns>
    Task<WorkspaceCommandResult> ExecuteCommandAsync(
        WorkspaceCommandRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an allowed Python script inside the requested workspace directory.
    /// </summary>
    /// <param name="request">The script path, arguments, working directory, and execution options.</param>
    /// <param name="cancellationToken">Token used to cancel the process operation.</param>
    /// <returns>A task that completes with exit code, bounded output, and timeout information.</returns>
    Task<WorkspaceCommandResult> ExecutePythonAsync(
        WorkspacePythonRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates policy-bound clients for workspace access contexts.
/// </summary>
public interface IWorkspaceClientFactory
{
    /// <summary>
    /// Creates a client bound to the workspace and policy in the supplied context.
    /// </summary>
    /// <param name="context">The workspace, actor, session, and operation policy to bind to the client.</param>
    /// <returns>A client that enforces the supplied access context.</returns>
    IWorkspaceClient Create(WorkspaceAccessContext context);
}

/// <summary>
/// Provides tenant-aware storage operations for workspace metadata and content.
/// </summary>
public interface IWorkspaceStorage
{
    /// <summary>
    /// Creates workspace metadata and its backing storage area.
    /// </summary>
    /// <param name="request">The owner, display name, owner type, and optional agent template.</param>
    /// <param name="cancellationToken">Token used to cancel creation.</param>
    /// <returns>A task that completes with the created workspace descriptor.</returns>
    Task<WorkspaceDescriptor> CreateAsync(
        WorkspaceCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves one workspace descriptor by identifier.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier to locate.</param>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns>The descriptor, or <see langword="null"/> when no matching workspace exists.</returns>
    Task<WorkspaceDescriptor?> GetAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Lists workspace descriptors visible to the current storage boundary.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>A task that completes with the available workspace descriptors.</returns>
    Task<IReadOnlyList<WorkspaceDescriptor>> ListWorkspacesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entries below a workspace-relative directory.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="relativePath">The workspace-relative directory path.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>A task that completes with the visible entries.</returns>
    Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
        string workspaceId,
        string relativePath = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a workspace-relative path to its storage path after applying workspace boundary checks.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="relativePath">The workspace-relative path to resolve.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>A task that completes with the resolved storage path.</returns>
    Task<string> ResolveFilePathAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a text file using the supplied maximum byte limit.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="relativePath">The workspace-relative file path.</param>
    /// <param name="maxBytes">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>A task that completes with the decoded file content.</returns>
    Task<string> ReadFileAsync(
        string workspaceId,
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metadata for a workspace-relative file or directory.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="relativePath">The workspace-relative path to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns>A task that completes with the current metadata.</returns>
    Task<WorkspaceFileMetadata> GetFileInfoAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads raw file bytes using the supplied maximum byte limit.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="relativePath">The workspace-relative file path.</param>
    /// <param name="maxBytes">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>A task that completes with file metadata and bytes.</returns>
    Task<WorkspaceFileContent> ReadFileBytesAsync(
        string workspaceId,
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a bounded line window from a workspace text file.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The line range, byte limit, and relative file path.</param>
    /// <param name="cancellationToken">Token used to cancel scanning.</param>
    /// <returns>A task that completes with the requested text window and continuation metadata.</returns>
    Task<WorkspaceTextWindow> ReadTextWindowAsync(
        string workspaceId,
        WorkspaceTextWindowRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes text content to a workspace-relative file.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="relativePath">The workspace-relative file path.</param>
    /// <param name="content">The text content to write.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    Task WriteFileAsync(
        string workspaceId,
        string relativePath,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes raw bytes using the request's create, overwrite, and entity-tag rules.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The target path, content, and concurrency options.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    /// <returns>A task that completes with the resulting file metadata.</returns>
    Task<WorkspaceWriteFileResult> WriteFileBytesAsync(
        string workspaceId,
        WorkspaceWriteFileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams content into a workspace-relative file without requiring the whole payload in memory.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The target path and expected content-length/concurrency options.</param>
    /// <param name="content">The readable content stream owned by the caller.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    /// <returns>A task that completes with the resulting file metadata.</returns>
    Task<WorkspaceWriteFileResult> WriteFileStreamAsync(
        string workspaceId,
        WorkspaceStreamWriteRequest request,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a directory below the workspace root.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="relativePath">The workspace-relative directory path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CreateDirectoryAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workspace entry according to the supplied recursive and entity-tag rules.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The target path and deletion options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task DeleteEntryAsync(
        string workspaceId,
        WorkspaceDeleteEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a workspace entry according to the supplied overwrite and entity-tag rules.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The source, target, overwrite, and concurrency options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task MoveEntryAsync(
        string workspaceId,
        WorkspaceMoveEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a workspace entry according to the supplied overwrite and entity-tag rules.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The source, target, overwrite, and concurrency options.</param>
    /// <param name="cancellationToken">Token used to cancel the copy.</param>
    Task CopyEntryAsync(
        string workspaceId,
        WorkspaceCopyEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches workspace paths and content using the supplied search constraints.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The path, content, glob, regular-expression, and result-limit options.</param>
    /// <param name="cancellationToken">Token used to cancel traversal.</param>
    /// <returns>A task that completes with matching workspace-relative results.</returns>
    Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
        string workspaceId,
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an exact-text patch after checking the optional hash and expected replacement count.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The target file, old text, replacement text, and concurrency checks.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that reports whether the patch was applied.</returns>
    Task<WorkspacePatchResult> ApplyPatchAsync(
        string workspaceId,
        WorkspacePatchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command allowed by the workspace policy.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The command, arguments, working directory, and execution options.</param>
    /// <param name="cancellationToken">Token used to cancel the process.</param>
    /// <returns>A task that completes with exit code, bounded output, and timeout information.</returns>
    Task<WorkspaceCommandResult> ExecuteCommandAsync(
        string workspaceId,
        WorkspaceCommandRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a Python script allowed by the workspace policy.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The script path, arguments, working directory, and execution options.</param>
    /// <param name="cancellationToken">Token used to cancel the process.</param>
    /// <returns>A task that completes with exit code, bounded output, and timeout information.</returns>
    Task<WorkspaceCommandResult> ExecutePythonAsync(
        string workspaceId,
        WorkspacePythonRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the workspace metadata and backing content.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier to delete.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    Task DeleteAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the file, directory, and byte usage for a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier to measure.</param>
    /// <param name="cancellationToken">Token used to cancel traversal.</param>
    /// <returns>A task that completes with usage counters and the configured quota.</returns>
    Task<WorkspaceUsageStatistics> GetUsageAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes user content from a workspace while preserving the workspace metadata boundary.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier whose content should be cleared.</param>
    /// <param name="cancellationToken">Token used to cancel traversal and deletion.</param>
    Task ClearContentAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
