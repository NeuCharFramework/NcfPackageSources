/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkspaceStorage.cs
    文件功能描述：工作区文件与命令存储
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.1 按策略执行工作区文件访问与命令

    修改标识：Senparc - 20261028
    修改描述：多租户模式下元数据读写在独立作用域中还原当前请求租户上下文

    修改标识：Senparc - 20261102
    修改描述：增加流式上传落盘与租户可见性校验

    修改标识：Senparc - 20261103
    修改描述：v0.2.2 add streamed large-file Workspace uploads with tenant-aware ownership checks

    修改标识：Senparc - 20261109
    修改描述：增加工作区用量统计，以及保留清单的内容清空

----------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Workspace.Domain.Services;
using System.Text;

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
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _streamWriteLocks = new(StringComparer.Ordinal);
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public WorkspaceStorage(
        IOptions<WorkspaceOptions> options,
        IHostEnvironment environment,
        ILogger<WorkspaceStorage> logger,
        IServiceScopeFactory scopeFactory,
        WorkspaceAgentConnectionPool agentPool,
        WorkspaceChangeHub changeHub,
        IHttpContextAccessor? httpContextAccessor = null)
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
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 捕获当前请求的租户快照（非请求上下文返回未匹配快照，保持 fail-closed）。
    /// </summary>
    private RequestTenantSnapshot CaptureCurrentTenant()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        return httpContext == null
            ? RequestTenantSnapshot.Unmatched
            : RequestTenantSnapshot.Capture(httpContext.RequestServices);
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
        var descriptor = await JsonSerializer.DeserializeAsync<WorkspaceDescriptor>(
            stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
        if (descriptor == null)
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        CaptureCurrentTenant().Restore(scope.ServiceProvider);
        var metadataStore = scope.ServiceProvider.GetRequiredService<IWorkspaceMetadataStore>();
        return await metadataStore.IsVisibleAsync(workspaceId, cancellationToken).ConfigureAwait(false)
            ? descriptor
            : null;
    }

    private async Task PersistMetadataAsync(WorkspaceDescriptor descriptor, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        CaptureCurrentTenant().Restore(scope.ServiceProvider);
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

    public async Task<WorkspaceTextWindow> ReadTextWindowAsync(
        string workspaceId,
        WorkspaceTextWindowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        return await _agentPool
            .InvokeAsync<WorkspaceTextWindowRequest, WorkspaceTextWindow>(
                workspaceId,
                workspaceRoot,
                WorkspaceAgentOperations.ReadTextWindow,
                request,
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

    public async Task<WorkspaceWriteFileResult> WriteFileStreamAsync(
        string workspaceId,
        WorkspaceStreamWriteRequest request,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
        {
            throw new ArgumentException("Workspace upload stream must be readable.", nameof(content));
        }

        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        if (request.ContentLength is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.ContentLength));
        }

        if (request.ContentLength is > 0 && request.ContentLength > _options.MaxUploadFileBytes)
        {
            throw new InvalidOperationException(
                $"Workspace upload exceeds the {_options.MaxUploadFileBytes} byte upload limit.");
        }

        var writeLock = _streamWriteLocks.GetOrAdd(workspaceId, _ => new SemaphoreSlim(1, 1));
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = ResolvePath(workspaceRoot, request.RelativePath);
            EnsureNoSymlink(workspaceRoot, path);
            var directory = Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("Workspace file has no parent directory.");
            EnsureNoSymlink(workspaceRoot, directory);

            var exists = File.Exists(path);
            if (Directory.Exists(path))
            {
                throw new IOException("Workspace upload target is a directory.");
            }

            if (!exists && !request.Create)
            {
                throw new FileNotFoundException("Workspace file does not exist.", request.RelativePath);
            }

            if (exists && !request.Overwrite)
            {
                throw new IOException("Workspace file already exists.");
            }

            if (exists && !string.IsNullOrWhiteSpace(request.ExpectedETag))
            {
                var current = await CreateFileMetadataAsync(workspaceRoot, path, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.Equals(current.ETag, request.ExpectedETag, StringComparison.Ordinal))
                {
                    throw new WorkspaceConflictException(
                        "Workspace file changed after it was read. Reload the file before saving.");
                }
            }

            var existingLength = exists ? new FileInfo(path).Length : 0;
            var currentBytes = GetWorkspaceBytes(workspaceRoot);
            if (request.ContentLength is { } declaredLength
                && currentBytes - existingLength + declaredLength > _options.MaxWorkspaceBytes)
            {
                throw new InvalidOperationException("Workspace quota exceeded.");
            }

            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(
                directory,
                $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.ncf-upload-tmp");
            long totalBytes = 0;
            try
            {
                await using (var target = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    128 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    var buffer = new byte[128 * 1024];
                    int read;
                    while ((read = await content.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        totalBytes += read;
                        if (totalBytes > _options.MaxUploadFileBytes)
                        {
                            throw new InvalidOperationException(
                                $"Workspace upload exceeds the {_options.MaxUploadFileBytes} byte upload limit.");
                        }

                        await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    }

                    await target.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                if (request.ContentLength is { } expectedLength && totalBytes != expectedLength)
                {
                    throw new InvalidDataException(
                        $"Workspace upload length mismatch: expected {expectedLength}, received {totalBytes}.");
                }

                if (request.ContentLength is null
                    && currentBytes - existingLength + totalBytes > _options.MaxWorkspaceBytes)
                {
                    throw new InvalidOperationException("Workspace quota exceeded.");
                }

                File.Move(temporaryPath, path, overwrite: request.Overwrite);
                var metadata = await CreateFileMetadataAsync(workspaceRoot, path, cancellationToken)
                    .ConfigureAwait(false);
                _changeHub.Publish(
                    workspaceId,
                    request.RelativePath,
                    exists ? "changed" : "created",
                    etag: metadata.ETag);
                _logger.LogInformation(
                    "Workspace stream file written: {WorkspaceId}, RelativePath={RelativePath}, Bytes={Bytes}",
                    workspaceId,
                    request.RelativePath,
                    totalBytes);
                return new WorkspaceWriteFileResult(metadata, !exists);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        finally
        {
            writeLock.Release();
        }
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
        var descriptor = await GetAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        using (var referenceScope = _scopeFactory.CreateScope())
        {
            CaptureCurrentTenant().Restore(referenceScope.ServiceProvider);
            foreach (var provider in referenceScope.ServiceProvider.GetServices<IWorkspaceReferenceProvider>())
            {
                var references = await provider.GetReferencesAsync(workspaceId, cancellationToken)
                    .ConfigureAwait(false);
                if (references.Count == 0)
                {
                    continue;
                }

                var referenceSummary = string.Join("、", references.Take(5).Select(reference =>
                    $"{reference.ReferenceType}:{reference.ReferenceId}"));
                if (references.Count > 5)
                {
                    referenceSummary += $" 等 {references.Count} 项";
                }

                throw new InvalidOperationException(
                    $"工作区“{workspaceId}”仍被引用，不能删除：{referenceSummary}。请先解除业务引用。");
            }
        }

        await _agentPool.StopAsync(workspaceId).ConfigureAwait(false);

        var stagedPath = path + $".deleting-{Guid.NewGuid():N}";
        Directory.Move(path, stagedPath);
        var metadataDeleted = false;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            CaptureCurrentTenant().Restore(scope.ServiceProvider);
            var metadataStore = scope.ServiceProvider.GetRequiredService<IWorkspaceMetadataStore>();
            await metadataStore.DeleteAsync(workspaceId, cancellationToken).ConfigureAwait(false);
            metadataDeleted = true;

            Directory.Delete(stagedPath, recursive: true);
            _logger.LogWarning("Workspace deleted: {WorkspaceId}", workspaceId);
        }
        catch
        {
            if (metadataDeleted && descriptor != null)
            {
                try
                {
                    await PersistMetadataAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception restoreException)
                {
                    _logger.LogError(
                        restoreException,
                        "Failed to restore workspace metadata after deletion rollback: {WorkspaceId}",
                        workspaceId);
                }
            }

            if (Directory.Exists(stagedPath) && !Directory.Exists(path))
            {
                Directory.Move(stagedPath, path);
            }

            throw;
        }
    }

    public async Task<WorkspaceUsageStatistics> GetUsageAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var path = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(path);
        var descriptor = await GetAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        if (descriptor == null)
        {
            throw new DirectoryNotFoundException("Workspace does not exist.");
        }

        return await Task.Run(
            () => WorkspaceDirectoryMaintenance.Measure(path, _options.MaxWorkspaceBytes, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearContentAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var path = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(path);
        var descriptor = await GetAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        if (descriptor == null)
        {
            throw new DirectoryNotFoundException("Workspace does not exist.");
        }

        var writeLock = _streamWriteLocks.GetOrAdd(workspaceId, _ => new SemaphoreSlim(1, 1));
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _agentPool.StopAsync(workspaceId).ConfigureAwait(false);
            await Task.Run(
                () => WorkspaceDirectoryMaintenance.ResetToInitialLayout(path, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            await _agentPool.StopAsync(workspaceId).ConfigureAwait(false);

            using var scope = _scopeFactory.CreateScope();
            CaptureCurrentTenant().Restore(scope.ServiceProvider);
            var metadataStore = scope.ServiceProvider.GetRequiredService<IWorkspaceMetadataStore>();
            await metadataStore.TouchAsync(workspaceId, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "Workspace content cleared back to the initial layout: {WorkspaceId}, OwnerId={OwnerId}",
                workspaceId,
                descriptor.OwnerId);
        }
        finally
        {
            writeLock.Release();
        }
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

    private static string ResolvePath(string workspaceRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidOperationException("Workspace root cannot be modified as a file entry.");
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Workspace paths must be relative.");
        }

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment is "." or ".."))
        {
            throw new InvalidOperationException("Workspace paths cannot contain traversal segments.");
        }

        var path = Path.GetFullPath(Path.Combine(workspaceRoot, normalized));
        var prefix = workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!path.StartsWith(prefix, comparison))
        {
            throw new InvalidOperationException("Workspace path escapes its root.");
        }

        var relative = Path.GetRelativePath(workspaceRoot, path)
            .Replace(Path.DirectorySeparatorChar, '/');
        var firstSegment = relative.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.Equals(firstSegment, ".workspace", StringComparison.OrdinalIgnoreCase)
            || string.Equals(firstSegment, ".workspace.json", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Workspace internal metadata is not externally accessible.");
        }

        return path;
    }

    private static void EnsureNoSymlink(string workspaceRoot, string path)
    {
        var relative = Path.GetRelativePath(workspaceRoot, path);
        var current = workspaceRoot;
        if (relative == ".")
        {
            return;
        }

        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current))
                && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("Workspace paths cannot traverse symbolic links.");
            }
        }
    }

    private static async Task<WorkspaceFileMetadata> CreateFileMetadataAsync(
        string workspaceRoot,
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Workspace file does not exist.", path);
        }

        var info = new FileInfo(path);
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        var relativePath = Path.GetRelativePath(workspaceRoot, path)
            .Replace(Path.DirectorySeparatorChar, '/');
        return new WorkspaceFileMetadata(
            relativePath,
            false,
            info.Length,
            info.CreationTimeUtc,
            info.LastWriteTimeUtc,
            $"\"{Convert.ToHexString(hash)}\"");
    }

    private static long GetWorkspaceBytes(string workspaceRoot)
    {
        var total = 0L;
        var pending = new Stack<string>();
        pending.Push(workspaceRoot);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    pending.Push(path);
                }
                else
                {
                    total = checked(total + new FileInfo(path).Length);
                }
            }
        }

        return total;
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
