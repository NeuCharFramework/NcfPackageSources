/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkspaceStorage.cs
    文件功能描述：工作区文件与命令存储
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.1 按策略执行工作区文件访问与命令

----------------------------------------------------------------*/

using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Xncf.Workspace.Domain.Services;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceStorage : IWorkspaceStorage
{
    private const string ManifestName = ".workspace.json";
    private readonly WorkspaceOptions _options;
    private readonly string _rootPath;
    private readonly ILogger<WorkspaceStorage> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkspaceAgentConnectionPool _agentPool;
    private readonly WorkspaceChangeHub _changeHub;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public WorkspaceStorage(
        IOptions<WorkspaceOptions> options,
        IHostEnvironment environment,
        ILogger<WorkspaceStorage> logger,
        IServiceScopeFactory scopeFactory,
        WorkspaceAgentConnectionPool agentPool,
        WorkspaceChangeHub changeHub)
    {
        _options = options.Value;
        _rootPath = Path.GetFullPath(Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(environment.ContentRootPath, _options.RootPath));
        Directory.CreateDirectory(_rootPath);
        _logger = logger;
        _scopeFactory = scopeFactory;
        _agentPool = agentPool;
        _changeHub = changeHub;
    }

    public async Task<WorkspaceDescriptor> CreateAsync(
        WorkspaceCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OwnerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.OwnerId));
        }

        var name = NormalizeName(request.Name);
        var id = $"ws_{Guid.NewGuid():N}";
        var path = GetWorkspaceRoot(id);
        Directory.CreateDirectory(path);
        Directory.CreateDirectory(Path.Combine(path, "input"));
        Directory.CreateDirectory(Path.Combine(path, "output"));
        Directory.CreateDirectory(Path.Combine(path, "logs"));
        Directory.CreateDirectory(Path.Combine(path, ".workspace"));

        var now = DateTime.UtcNow;
        var descriptor = new WorkspaceDescriptor(
            id,
            request.OwnerId,
            name,
            path,
            now,
            now,
            request.OwnerType,
            request.AgentTemplateId);
        await WriteManifestAsync(path, descriptor, cancellationToken).ConfigureAwait(false);
        await PersistMetadataAsync(descriptor, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Workspace created: {WorkspaceId}, OwnerId={OwnerId}", id, request.OwnerId);
        return descriptor;
    }

    public async Task<WorkspaceDescriptor?> GetAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var path = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(path);
        var manifestPath = Path.Combine(path, ManifestName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(manifestPath);
        return await JsonSerializer.DeserializeAsync<WorkspaceDescriptor>(
            stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task PersistMetadataAsync(WorkspaceDescriptor descriptor, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var metadataStore = scope.ServiceProvider.GetRequiredService<IWorkspaceMetadataStore>();
        await metadataStore.UpsertAsync(descriptor, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkspaceDescriptor>> ListWorkspacesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<WorkspaceDescriptor>();
        foreach (var directory in Directory.EnumerateDirectories(_rootPath, "ws_*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descriptor = await GetAsync(Path.GetFileName(directory), cancellationToken).ConfigureAwait(false);
            if (descriptor != null)
            {
                result.Add(descriptor);
            }
        }

        return result
            .OrderByDescending(item => item.LastAccessedUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
        string workspaceId,
        string relativePath = "",
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        return await _agentPool
            .InvokeAsync<WorkspaceAgentListRequest, WorkspaceEntry[]>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.List,
                new WorkspaceAgentListRequest(relativePath),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<string> ResolveFilePathAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = GetWorkspaceRoot(workspaceId);
        throw new NotSupportedException(
            "Workspace physical paths are not exposed. Use IWorkspaceClient file operations.");
    }

    public async Task<string> ReadFileAsync(
        string workspaceId,
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        var file = await ReadFileBytesAsync(
            workspaceId,
            relativePath,
            maxBytes,
            cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(file.Content);
    }

    public async Task<WorkspaceFileMetadata> GetFileInfoAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        return await _agentPool
            .InvokeAsync<WorkspaceAgentPathRequest, WorkspaceFileMetadata>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.Stat,
                new WorkspaceAgentPathRequest(relativePath),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkspaceFileContent> ReadFileBytesAsync(
        string workspaceId,
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        return await _agentPool
            .InvokeAsync<WorkspaceAgentReadRequest, WorkspaceFileContent>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.Read,
                new WorkspaceAgentReadRequest(relativePath, maxBytes),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task WriteFileAsync(
        string workspaceId,
        string relativePath,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        await WriteFileBytesAsync(
            workspaceId,
            new WorkspaceWriteFileRequest(relativePath, Encoding.UTF8.GetBytes(content)),
            cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Workspace file written: {WorkspaceId}, RelativePath={RelativePath}", workspaceId, relativePath);
    }

    public async Task<WorkspaceWriteFileResult> WriteFileBytesAsync(
        string workspaceId,
        WorkspaceWriteFileRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        var result = await _agentPool
            .InvokeAsync<WorkspaceWriteFileRequest, WorkspaceWriteFileResult>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.Write,
                request,
                cancellationToken)
            .ConfigureAwait(false);
        _changeHub.Publish(
            workspaceId,
            request.RelativePath,
            result.Created ? "created" : "changed",
            etag: result.Metadata.ETag);
        _logger.LogInformation(
            "Workspace file written through Agent: {WorkspaceId}, RelativePath={RelativePath}",
            workspaceId,
            request.RelativePath);
        return result;
    }

    public async Task CreateDirectoryAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        await _agentPool
            .InvokeAsync<WorkspaceAgentPathRequest, WorkspaceAgentEmptyResult>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.CreateDirectory,
                new WorkspaceAgentPathRequest(relativePath),
                cancellationToken)
            .ConfigureAwait(false);
        _changeHub.Publish(workspaceId, relativePath, "created");
        _logger.LogInformation("Workspace directory created: {WorkspaceId}, RelativePath={RelativePath}", workspaceId, relativePath);
    }

    public async Task DeleteEntryAsync(
        string workspaceId,
        WorkspaceDeleteEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        await _agentPool
            .InvokeAsync<WorkspaceDeleteEntryRequest, WorkspaceAgentEmptyResult>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.Delete,
                request,
                cancellationToken)
            .ConfigureAwait(false);
        _changeHub.Publish(workspaceId, request.RelativePath, "deleted");
        _logger.LogWarning(
            "Workspace entry deleted through Agent: {WorkspaceId}, RelativePath={RelativePath}",
            workspaceId,
            request.RelativePath);
    }

    public async Task MoveEntryAsync(
        string workspaceId,
        WorkspaceMoveEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        await _agentPool
            .InvokeAsync<WorkspaceMoveEntryRequest, WorkspaceAgentEmptyResult>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.Move,
                request,
                cancellationToken)
            .ConfigureAwait(false);
        _changeHub.Publish(
            workspaceId,
            request.TargetRelativePath,
            "renamed",
            request.SourceRelativePath);
        _logger.LogInformation(
            "Workspace entry moved through Agent: {WorkspaceId}, Source={Source}, Target={Target}",
            workspaceId,
            request.SourceRelativePath,
            request.TargetRelativePath);
    }

    public async Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
        string workspaceId,
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        return await _agentPool
            .InvokeAsync<WorkspaceSearchRequest, WorkspaceSearchResult[]>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.Search,
                request,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkspacePatchResult> ApplyPatchAsync(
        string workspaceId,
        WorkspacePatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        var result = await _agentPool
            .InvokeAsync<WorkspacePatchRequest, WorkspacePatchResult>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.Patch,
                request,
                cancellationToken)
            .ConfigureAwait(false);
        if (result.Applied)
        {
            _changeHub.Publish(workspaceId, request.RelativePath, "changed");
        }

        return result;
    }

    public async Task<WorkspaceCommandResult> ExecuteCommandAsync(
        string workspaceId,
        WorkspaceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        return await _agentPool
            .InvokeAsync<WorkspaceCommandRequest, WorkspaceCommandResult>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.ExecuteCommand,
                request,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkspaceCommandResult> ExecutePythonAsync(
        string workspaceId,
        WorkspacePythonRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        return await _agentPool
            .InvokeAsync<WorkspacePythonRequest, WorkspaceCommandResult>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.ExecutePython,
                request,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(path);
        await _agentPool.StopAsync(workspaceId).ConfigureAwait(false);
        Directory.Delete(path, recursive: true);
        _logger.LogWarning("Workspace deleted: {WorkspaceId}", workspaceId);
    }

    private string GetWorkspaceRoot(string workspaceId)
    {
        if (string.IsNullOrWhiteSpace(workspaceId)
            || !workspaceId.StartsWith("ws_", StringComparison.Ordinal)
            || workspaceId.Length != 35
            || workspaceId.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '_')))
        {
            throw new ArgumentException("Invalid workspace identifier.", nameof(workspaceId));
        }

        return Path.Combine(_rootPath, workspaceId);
    }

    private static string NormalizeName(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? "Workspace" : name.Trim();
        if (normalized.Length > 100)
        {
            normalized = normalized[..100];
        }

        return normalized;
    }

    private static void EnsureWorkspaceExists(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException("Workspace does not exist.");
        }

        if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("Workspace root cannot be a symbolic link.");
        }
    }

    private async Task WriteManifestAsync(
        string path,
        WorkspaceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(path, ManifestName);
        await using var stream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(stream, descriptor, _jsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }
}
