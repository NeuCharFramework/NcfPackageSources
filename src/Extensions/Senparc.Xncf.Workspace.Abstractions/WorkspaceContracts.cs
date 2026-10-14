namespace Senparc.Xncf.Workspace.Abstractions;

public sealed record WorkspaceDescriptor(
    string Id,
    int OwnerId,
    string Name,
    string RootPath,
    DateTime CreatedUtc,
    DateTime LastAccessedUtc);

public sealed record WorkspaceEntry(
    string RelativePath,
    bool IsDirectory,
    long Length,
    DateTime LastWriteTimeUtc);

public sealed record WorkspaceCreateRequest(int OwnerId, string Name);

public sealed record WorkspaceCommandRequest(
    string Command,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory = "");

public sealed record WorkspaceCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);

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

    Task<WorkspaceCommandResult> ExecuteCommandAsync(
        string workspaceId,
        WorkspaceCommandRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
