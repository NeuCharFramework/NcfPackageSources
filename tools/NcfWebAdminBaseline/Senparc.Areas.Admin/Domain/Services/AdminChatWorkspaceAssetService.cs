/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatWorkspaceAssetService.cs
    File description: AdminChatWorkspaceAssetService related definitions and implementation

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Xncf.Workspace;
using Senparc.Xncf.Workspace.Abstractions;
using Microsoft.Extensions.Options;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record WorkspaceAssetDownload(byte[] Bytes, string FileName);

public sealed class AdminChatWorkspaceAssetService
{
    public const int MaxArchiveEntries = 1000;
    public const long MaxArchiveBytes = 64 * 1024 * 1024;
    private readonly AdminChatSessionService _sessions;
    private readonly IServiceProvider _services;
    private readonly WorkspaceAssetPreviewBuilder _previewBuilder;

    /// <summary>
    /// Initializes the service that authorizes, previews, and downloads files from
    /// administrator chat workspaces.
    /// </summary>
    /// <param name="sessions">The owner-scoped chat-session service.</param>
    /// <param name="services">The provider for workspace storage and options.</param>
    /// <param name="previewBuilder">The service that renders safe workspace previews.</param>
    public AdminChatWorkspaceAssetService(
        AdminChatSessionService sessions, IServiceProvider services, WorkspaceAssetPreviewBuilder previewBuilder)
    {
        _sessions = sessions;
        _services = services;
        _previewBuilder = previewBuilder;
    }

    public IWorkspaceStorage Storage => _services.GetService<IWorkspaceStorage>()
        ?? throw new InvalidOperationException("Workspace 模块尚不可用。");

    /// <summary>
    /// Authenticates a super-administrator and resolves an owned chat session with
    /// a workspace that belongs to the same administrator.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="user">The principal whose authentication, role, and owner ID are checked.</param>
    /// <param name="cancellationToken">Token reserved for the storage lookup.</param>
    /// <returns>The validated chat session.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the caller is not an authenticated super-administrator or the workspace
    /// is not owned by that administrator.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the session or workspace is missing.</exception>
    public async Task<AdminChatSession> GetSessionAsync(
        int sessionId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var isSuperAdmin = user.Claims.Where(claim => claim.Type == ClaimTypes.Role)
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Any(role => string.Equals(role.Trim(), Ncf.Service.Config.SYSROLE_ADMINISTRATOR_ROLE_CODE,
                StringComparison.OrdinalIgnoreCase));
        if (user.Identity?.IsAuthenticated != true || !isSuperAdmin
            || !int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerId) || ownerId <= 0)
            throw new UnauthorizedAccessException("只有有权限的超级管理员可以访问 Workspace 资产。");
        var session = await _sessions.GetSessionByIdAsync(sessionId, ownerId);
        if (session == null || string.IsNullOrEmpty(session.WorkspaceId))
            throw new FileNotFoundException("会话或 Workspace 不存在。");
        var workspace = await Storage.GetAsync(session.WorkspaceId, cancellationToken);
        if (workspace == null || workspace.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Workspace 不存在或不属于当前管理员。");
        return session;
    }

    /// <summary>
    /// Builds an HTML-safe preview for a path inside a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative path to preview.</param>
    /// <param name="cancellationToken">Token used to cancel storage and rendering.</param>
    /// <returns>The generated workspace HTML preview.</returns>
    public Task<WorkspaceHtmlPreview> BuildPreviewAsync(
        string workspaceId, string path, CancellationToken cancellationToken) =>
        _previewBuilder.BuildAsync(Storage, workspaceId, path, cancellationToken);

    /// <summary>
    /// Verifies that a persisted share still points to an existing workspace owned
    /// by the share owner and associated with the expected chat session.
    /// </summary>
    /// <param name="share">The persisted share descriptor.</param>
    /// <param name="cancellationToken">Token used to query workspace storage.</param>
    /// <returns><see langword="true"/> when the share remains usable.</returns>
    public async Task<bool> IsShareWorkspaceAvailableAsync(WorkspaceAssetShare share, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetSessionByIdAsync(share.SessionId, share.OwnerId);
        if (session?.WorkspaceId != share.WorkspaceId) return false;
        var workspace = await Storage.GetAsync(share.WorkspaceId, cancellationToken);
        return workspace?.OwnerId == share.OwnerId;
    }

    /// <summary>
    /// Downloads one file or packages a workspace directory into a bounded ZIP archive.
    /// Archive entry count, nesting, list truncation, and total byte limits are enforced.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative file or directory path.</param>
    /// <param name="cancellationToken">Token used to cancel storage and archive operations.</param>
    /// <returns>The downloaded bytes and a file name suitable for an HTTP response.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when directory depth, entry count, list completeness, or byte limits are exceeded.
    /// </exception>
    public async Task<WorkspaceAssetDownload> DownloadAsync(
        string workspaceId, string path, CancellationToken cancellationToken)
    {
        path = WorkspaceAssetPreviewBuilder.NormalizePath(path, allowRoot: true);
        var metadata = await Storage.GetFileInfoAsync(workspaceId, path, cancellationToken);
        var fileLimit = Math.Min((int)MaxArchiveBytes,
            _services.GetRequiredService<IOptions<WorkspaceOptions>>().Value.MaxFileReadBytes);
        if (!metadata.IsDirectory)
        {
            var file = await Storage.ReadFileBytesAsync(workspaceId, path, fileLimit, cancellationToken);
            return new WorkspaceAssetDownload(file.Content, path.Split('/').Last());
        }

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var directories = new Queue<(string Path, int Depth)>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            directories.Enqueue((path, 0));
            long totalBytes = 0;
            var count = 0;
            var listLimit = _services.GetRequiredService<IOptions<WorkspaceOptions>>().Value.MaxEntriesPerList;
            while (directories.TryDequeue(out var directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (directory.Depth > 32) throw new InvalidOperationException("目录嵌套超过 32 层，无法打包。");
                var entries = await Storage.ListAsync(workspaceId, directory.Path, cancellationToken);
                if (entries.Count >= listLimit)
                    throw new InvalidOperationException("目录列表可能被截断，请选择较小的子目录下载。");
                foreach (var entry in entries)
                {
                    var entryPath = WorkspaceAssetPreviewBuilder.NormalizePath(entry.RelativePath);
                    var prefix = directory.Path.Length == 0 ? "" : directory.Path + "/";
                    if (!entryPath.StartsWith(prefix, StringComparison.Ordinal)
                        || entryPath[prefix.Length..].Contains('/') || !seen.Add(entryPath))
                        throw new IOException("Workspace 返回了无效的目录条目。");
                    if (++count > MaxArchiveEntries)
                        throw new InvalidOperationException("ZIP 最多包含 1000 个文件和目录，请选择较小的子目录。");
                    var zipPath = path.Length == 0 ? entryPath : entryPath[(path.Length + 1)..];
                    if (entry.IsDirectory)
                    {
                        archive.CreateEntry(zipPath + "/");
                        directories.Enqueue((entryPath, directory.Depth + 1));
                    }
                    else
                    {
                        if (entry.Length > fileLimit || entry.Length > MaxArchiveBytes - totalBytes)
                            throw new InvalidOperationException("文件超过 Workspace 读取限制或 ZIP 原始内容超过 64 MB。");
                        var file = await Storage.ReadFileBytesAsync(workspaceId, entryPath, fileLimit, cancellationToken);
                        totalBytes += file.Content.Length;
                        if (totalBytes > MaxArchiveBytes) throw new InvalidOperationException("ZIP 原始内容超过 64 MB。");
                        await using var target = archive.CreateEntry(zipPath, CompressionLevel.Fastest).Open();
                        await target.WriteAsync(file.Content, cancellationToken);
                    }
                }
            }
        }
        var name = path.Length == 0 ? "workspace" : path.Split('/').Last();
        return new WorkspaceAssetDownload(output.ToArray(), name + ".zip");
    }
}
