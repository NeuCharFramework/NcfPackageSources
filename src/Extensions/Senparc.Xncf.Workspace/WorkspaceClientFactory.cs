/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceClientFactory.cs
    文件功能描述：带会话策略的统一 Workspace Client

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceClientFactory : IWorkspaceClientFactory
{
    private readonly IWorkspaceStorage _storage;

    public WorkspaceClientFactory(IWorkspaceStorage storage)
    {
        _storage = storage;
    }

    public IWorkspaceClient Create(WorkspaceAccessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.WorkspaceId))
        {
            throw new ArgumentException("Workspace identifier is required.", nameof(context));
        }

        return new WorkspaceClient(_storage, context);
    }

    private sealed class WorkspaceClient : IWorkspaceClient
    {
        private readonly IWorkspaceStorage _storage;
        private readonly WorkspaceAccessContext _context;

        public WorkspaceClient(IWorkspaceStorage storage, WorkspaceAccessContext context)
        {
            _storage = storage;
            _context = context;
        }

        public string WorkspaceId => _context.WorkspaceId;

        public WorkspacePolicy Policy => _context.Policy;

        public Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
            string relativePath = "",
            CancellationToken cancellationToken = default)
        {
            EnsureRead();
            return _storage.ListAsync(WorkspaceId, relativePath, cancellationToken);
        }

        public Task<WorkspaceFileMetadata> GetFileInfoAsync(
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            EnsureRead();
            return _storage.GetFileInfoAsync(WorkspaceId, relativePath, cancellationToken);
        }

        public Task<WorkspaceFileContent> ReadFileAsync(
            string relativePath,
            int maxBytes = 2 * 1024 * 1024,
            CancellationToken cancellationToken = default)
        {
            EnsureRead();
            var policyLimit = Policy.MaxBytes > 0
                ? (int)Math.Min(int.MaxValue, Policy.MaxBytes)
                : maxBytes;
            return _storage.ReadFileBytesAsync(
                WorkspaceId,
                relativePath,
                Math.Min(maxBytes, policyLimit),
                cancellationToken);
        }

        public Task<WorkspaceWriteFileResult> WriteFileAsync(
            WorkspaceWriteFileRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            if (Policy.MaxBytes > 0 && request.Content.LongLength > Policy.MaxBytes)
            {
                throw new InvalidOperationException("Workspace write exceeds the session policy limit.");
            }

            return _storage.WriteFileBytesAsync(WorkspaceId, request, cancellationToken);
        }

        public Task CreateDirectoryAsync(
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            return _storage.CreateDirectoryAsync(WorkspaceId, relativePath, cancellationToken);
        }

        public Task DeleteEntryAsync(
            WorkspaceDeleteEntryRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            return _storage.DeleteEntryAsync(WorkspaceId, request, cancellationToken);
        }

        public Task MoveEntryAsync(
            WorkspaceMoveEntryRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            return _storage.MoveEntryAsync(WorkspaceId, request, cancellationToken);
        }

        public Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
            WorkspaceSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureRead();
            return _storage.SearchAsync(WorkspaceId, request, cancellationToken);
        }

        public Task<WorkspacePatchResult> ApplyPatchAsync(
            WorkspacePatchRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            return _storage.ApplyPatchAsync(WorkspaceId, request, cancellationToken);
        }

        public Task<WorkspaceCommandResult> ExecuteCommandAsync(
            WorkspaceCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!Policy.AllowCommand)
            {
                throw new UnauthorizedAccessException("Workspace command execution is not allowed.");
            }

            var allowedCommands = Policy.AllowedCommands ?? Array.Empty<string>();
            var command = Path.GetFileName(request.Command);
            if (allowedCommands.Count > 0 &&
                !allowedCommands.Any(item =>
                    string.Equals(item, command, StringComparison.OrdinalIgnoreCase)))
            {
                throw new UnauthorizedAccessException("Workspace command is outside the session allowlist.");
            }

            return _storage.ExecuteCommandAsync(WorkspaceId, request, cancellationToken);
        }

        public Task<WorkspaceCommandResult> ExecutePythonAsync(
            WorkspacePythonRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!Policy.AllowPython)
            {
                throw new UnauthorizedAccessException("Workspace Python execution is not allowed.");
            }

            return _storage.ExecutePythonAsync(WorkspaceId, request, cancellationToken);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private void EnsureRead()
        {
            if (!Policy.AllowRead)
            {
                throw new UnauthorizedAccessException("Workspace read access is not allowed.");
            }
        }

        private void EnsureWrite()
        {
            if (!Policy.AllowWrite)
            {
                throw new UnauthorizedAccessException("Workspace write access is not allowed.");
            }
        }
    }
}
