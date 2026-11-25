/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceClientFactory.cs
    File description: unified Workspace client with session policies

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.2 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceClientFactory : IWorkspaceClientFactory
{
    private readonly IWorkspaceStorage _storage;

    /// <summary>
    /// Initializes a factory over the tenant-aware workspace storage boundary.
    /// </summary>
    /// <param name="storage">Storage implementation used by created clients.</param>
    public WorkspaceClientFactory(IWorkspaceStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc />
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

        /// <summary>
        /// Initializes a client bound to one workspace access context.
        /// </summary>
        /// <param name="storage">Storage implementation used for operations.</param>
        /// <param name="context">Workspace identity, actor, session, and policy.</param>
        public WorkspaceClient(IWorkspaceStorage storage, WorkspaceAccessContext context)
        {
            _storage = storage;
            _context = context;
        }

        public string WorkspaceId => _context.WorkspaceId;

        public WorkspacePolicy Policy => _context.Policy;

        /// <inheritdoc />
        public Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
            string relativePath = "",
            CancellationToken cancellationToken = default)
        {
            EnsureRead();
            return _storage.ListAsync(WorkspaceId, relativePath, cancellationToken);
        }

        /// <inheritdoc />
        public Task<WorkspaceFileMetadata> GetFileInfoAsync(
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            EnsureRead();
            return _storage.GetFileInfoAsync(WorkspaceId, relativePath, cancellationToken);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Task CreateDirectoryAsync(
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            return _storage.CreateDirectoryAsync(WorkspaceId, relativePath, cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteEntryAsync(
            WorkspaceDeleteEntryRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            return _storage.DeleteEntryAsync(WorkspaceId, request, cancellationToken);
        }

        /// <inheritdoc />
        public Task MoveEntryAsync(
            WorkspaceMoveEntryRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            return _storage.MoveEntryAsync(WorkspaceId, request, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
            WorkspaceSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureRead();
            return _storage.SearchAsync(WorkspaceId, request, cancellationToken);
        }

        /// <inheritdoc />
        public Task<WorkspacePatchResult> ApplyPatchAsync(
            WorkspacePatchRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureWrite();
            return _storage.ApplyPatchAsync(WorkspaceId, request, cancellationToken);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <summary>
        /// Enforces the access-context read permission before delegating a read operation.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown when the session does not allow reads.</exception>
        private void EnsureRead()
        {
            if (!Policy.AllowRead)
            {
                throw new UnauthorizedAccessException("Workspace read access is not allowed.");
            }
        }

        /// <summary>
        /// Enforces the access-context write permission before delegating a mutation.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown when the session does not allow writes.</exception>
        private void EnsureWrite()
        {
            if (!Policy.AllowWrite)
            {
                throw new UnauthorizedAccessException("Workspace write access is not allowed.");
            }
        }
    }
}
