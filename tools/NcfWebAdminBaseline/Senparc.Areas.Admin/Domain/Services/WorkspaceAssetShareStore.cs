/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceAssetShareStore.cs
    File description: WorkspaceAssetShareStore related definitions and implementation

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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record WorkspaceAssetShare(
    string Id, string Token, int OwnerId, int SessionId, string WorkspaceId,
    string Path, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string Html);

public sealed class WorkspaceAssetShareStore : IDisposable
{
    private static readonly Regex ShareId = new(@"^[1-9][0-9]{0,9}-[a-f0-9]{32}$", RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));
    private readonly string _root;
    private readonly IDataProtector _protector;
    private readonly ILogger<WorkspaceAssetShareStore> _logger;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly PartitionedRateLimiter<string> _publicLimiter = PartitionedRateLimiter.Create<string, string>(
        key => RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
        }));

    /// <summary>
    /// Initializes protected on-disk workspace-share storage outside the public web root.
    /// </summary>
    /// <param name="environment">The host environment used to locate application data.</param>
    /// <param name="protection">The Data Protection provider used to encrypt share descriptors.</param>
    /// <param name="logger">The share-storage diagnostic logger.</param>
    /// <param name="timeProvider">Optional clock used for expiration checks and tests.</param>
    /// <exception cref="InvalidOperationException">Thrown when the selected storage path is inside <c>wwwroot</c>.</exception>
    public WorkspaceAssetShareStore(
        IWebHostEnvironment environment, IDataProtectionProvider protection,
        ILogger<WorkspaceAssetShareStore> logger, TimeProvider timeProvider = null)
    {
        _root = System.IO.Path.Combine(environment.ContentRootPath, "App_Data", "WorkspaceAssetShares");
        if (!string.IsNullOrEmpty(environment.WebRootPath)
            && (string.Equals(System.IO.Path.GetFullPath(_root),
                    System.IO.Path.GetFullPath(environment.WebRootPath).TrimEnd(System.IO.Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase)
                || System.IO.Path.GetFullPath(_root).StartsWith(
                    System.IO.Path.GetFullPath(environment.WebRootPath).TrimEnd(System.IO.Path.DirectorySeparatorChar)
                    + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("分享存储不能位于 wwwroot 中。");
        _protector = protection.CreateProtector("NCF.AdminChat.WorkspaceAssetShare.v1");
        _logger = logger;
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Attempts to acquire one public-share request from the caller's fixed-window rate limiter.
    /// </summary>
    /// <param name="remoteAddress">The remote address used as the limiter partition key.</param>
    /// <returns><see langword="true"/> when the request is within the current allowance.</returns>
    public bool TryAcquirePublicRequest(string remoteAddress)
    {
        using var lease = _publicLimiter.AttemptAcquire(remoteAddress);
        return lease.IsAcquired;
    }

    /// <summary>
    /// Encrypts and atomically persists an expiring HTML workspace snapshot for its owner.
    /// </summary>
    /// <param name="ownerId">The administrator who owns the share.</param>
    /// <param name="sessionId">The chat session associated with the share.</param>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative path represented by the snapshot.</param>
    /// <param name="html">The already-rendered HTML snapshot.</param>
    /// <param name="expiresInHours">The allowed lifetime: 1, 24, or 168 hours.</param>
    /// <param name="cancellationToken">Token used to cancel persistence.</param>
    /// <returns>The persisted share descriptor containing its public token.</returns>
    /// <exception cref="ArgumentException">Thrown when ownership, lifetime, path, or size validation fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the owner has reached the active-share limit.</exception>
    public async Task<WorkspaceAssetShare> CreateAsync(
        int ownerId, int sessionId, string workspaceId, string path, string html,
        int expiresInHours, CancellationToken cancellationToken = default)
    {
        if (ownerId <= 0 || sessionId <= 0 || string.IsNullOrWhiteSpace(workspaceId))
            throw new ArgumentException("分享必须属于有效的管理员和会话。");
        if (expiresInHours is not (1 or 24 or 168))
            throw new ArgumentException("分享有效期只能是 1 小时、24 小时或 7 天。");
        WorkspaceAssetPreviewBuilder.NormalizePath(path);
        if (Encoding.UTF8.GetByteCount(html) > WorkspaceAssetPreviewBuilder.MaxHtmlBytes)
            throw new ArgumentException("分享快照过大。");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = GetOwnerDirectory(ownerId);
            Directory.CreateDirectory(directory);
            var shares = await ListCoreAsync(ownerId, cancellationToken);
            if (shares.Count >= 10) throw new InvalidOperationException("每位管理员最多保留 10 个有效分享，请先撤销旧分享。");
            var now = _time.GetUtcNow();
            var share = new WorkspaceAssetShare(
                $"{ownerId}-{Guid.NewGuid():N}", WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
                ownerId, sessionId, workspaceId, path, now, now.AddHours(expiresInHours), html);
            var bytes = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(share));
            var filePath = GetFilePath(share.Id, share.Token);
            var temporaryPath = filePath + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
                File.Move(temporaryPath, filePath);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            _logger.LogInformation("Workspace HTML snapshot shared: Owner={OwnerId}, Session={SessionId}, Share={ShareId}, Expires={ExpiresAt}",
                ownerId, sessionId, share.Id, share.ExpiresAt);
            return share;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Lists non-expired shares for one owner/session/workspace boundary without returning HTML bodies.
    /// </summary>
    /// <param name="ownerId">The administrator who owns the shares.</param>
    /// <param name="sessionId">The associated chat session.</param>
    /// <param name="workspaceId">The associated workspace.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>Share metadata ordered newest first.</returns>
    public async Task<IReadOnlyList<WorkspaceAssetShare>> ListAsync(
        int ownerId, int sessionId, string workspaceId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await ListCoreAsync(ownerId, cancellationToken))
                .Where(share => share.SessionId == sessionId && share.WorkspaceId == workspaceId)
                .Select(share => share with { Html = "" })
                .OrderByDescending(share => share.CreatedAt).ToArray();
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Deletes a matching persisted share descriptor.
    /// </summary>
    /// <param name="ownerId">The administrator who owns the share.</param>
    /// <param name="sessionId">The associated chat session.</param>
    /// <param name="workspaceId">The associated workspace.</param>
    /// <param name="shareId">The share identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><see langword="true"/> when a matching share was removed.</returns>
    public async Task<bool> RevokeAsync(
        int ownerId, int sessionId, string workspaceId, string shareId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var share = (await ListCoreAsync(ownerId, cancellationToken))
                .FirstOrDefault(item => item.Id == shareId && item.SessionId == sessionId && item.WorkspaceId == workspaceId);
            if (share == null) return false;
            File.Delete(GetFilePath(share.Id, share.Token));
            _logger.LogInformation("Workspace HTML snapshot revoked: Owner={OwnerId}, Share={ShareId}", ownerId, shareId);
            return true;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Reads and decrypts a public share after validating its identifier/token format,
    /// checking expiration, and comparing the token in constant time.
    /// </summary>
    /// <param name="id">The public share identifier.</param>
    /// <param name="token">The public share token.</param>
    /// <param name="cancellationToken">Token used to cancel storage access.</param>
    /// <returns>The share descriptor, or <see langword="null"/> for invalid, missing, expired, or mismatched shares.</returns>
    public async Task<WorkspaceAssetShare?> ReadPublicAsync(
        string id, string token, CancellationToken cancellationToken = default)
    {
        if (id == null || !ShareId.IsMatch(id) || token == null || token.Length != 43
            || !token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            return null;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = GetFilePath(id, token);
            if (!File.Exists(path)) return null;
            var share = await ReadCoreAsync(path, cancellationToken);
            if (share.ExpiresAt <= _time.GetUtcNow())
            {
                File.Delete(path);
                return null;
            }
            return share.Id == id && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(share.Token), Encoding.ASCII.GetBytes(token)) ? share : null;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Reads all non-expired protected share files for an owner and removes expired files.
    /// </summary>
    /// <param name="ownerId">The owner whose share directory is scanned.</param>
    /// <param name="cancellationToken">Token used to cancel protected-file reads.</param>
    /// <returns>The valid shares belonging to the owner.</returns>
    private async Task<List<WorkspaceAssetShare>> ListCoreAsync(int ownerId, CancellationToken cancellationToken)
    {
        var result = new List<WorkspaceAssetShare>();
        var directory = GetOwnerDirectory(ownerId);
        if (!Directory.Exists(directory)) return result;
        foreach (var path in Directory.EnumerateFiles(directory, "*.protected"))
        {
            var share = await ReadCoreAsync(path, cancellationToken);
            if (share.ExpiresAt <= _time.GetUtcNow())
                File.Delete(path);
            else if (share.OwnerId == ownerId)
                result.Add(share);
        }
        return result;
    }

    /// <summary>
    /// Reads, decrypts, and deserializes one protected share file while enforcing
    /// the maximum persisted payload size.
    /// </summary>
    /// <param name="path">The protected share file path.</param>
    /// <param name="cancellationToken">Token used to cancel file reads.</param>
    /// <returns>The decrypted share descriptor.</returns>
    /// <exception cref="IOException">Thrown when the file is oversized, invalid, or cannot be decrypted.</exception>
    private async Task<WorkspaceAssetShare> ReadCoreAsync(string path, CancellationToken cancellationToken)
    {
        if (new FileInfo(path).Length > WorkspaceAssetPreviewBuilder.MaxHtmlBytes * 6 + 4096)
            throw new IOException("分享文件超过存储限制。");
        try
        {
            var protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<WorkspaceAssetShare>(_protector.Unprotect(protectedBytes))
                   ?? throw new IOException("分享文件内容无效。");
        }
        catch (Exception error) when (error is CryptographicException or JsonException)
        {
            _logger.LogError(error, "Workspace share cannot be decrypted. Check persistent Data Protection keys.");
            throw new IOException("无法解密分享，请检查持久化 Data Protection 密钥。", error);
        }
    }

    /// <summary>
    /// Resolves the private owner directory used for protected share files.
    /// </summary>
    /// <param name="ownerId">The positive administrator identifier.</param>
    /// <returns>The owner-specific storage directory.</returns>
    /// <exception cref="ArgumentException">Thrown when the owner identifier is not positive.</exception>
    private string GetOwnerDirectory(int ownerId)
    {
        if (ownerId <= 0) throw new ArgumentException("无效的分享所有者。");
        return System.IO.Path.Combine(_root, ownerId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Derives the deterministic protected-file path from a validated share ID and token.
    /// </summary>
    /// <param name="id">The share identifier containing the owner prefix.</param>
    /// <param name="token">The share token used to derive the file name.</param>
    /// <returns>The private protected-file path.</returns>
    /// <exception cref="ArgumentException">Thrown when the share ID format is invalid.</exception>
    private string GetFilePath(string id, string token)
    {
        if (!ShareId.IsMatch(id) || !int.TryParse(id.Split('-')[0], out var ownerId))
            throw new ArgumentException("无效的分享标识。");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        return System.IO.Path.Combine(GetOwnerDirectory(ownerId), $"{id}-{hash}.protected");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _publicLimiter.Dispose();
        _gate.Dispose();
    }
}
