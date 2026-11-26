/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawMediaStorageService.cs
    File description: WeixinClawMediaStorageService.cs implementation and project behavior.

    Created by: Senparc - 20260930

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw media handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawMediaStorageService
{
    private readonly string _rootPath;

    /// <summary>
    /// Initializes media storage below the application data directory.
    /// </summary>
    /// <param name="environment">Host environment used to resolve the storage root.</param>
    public WeixinClawMediaStorageService(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "WeixinClawMedia");
    }

    /// <summary>
    /// Saves inbound media under an account-scoped sanitized storage key.
    /// </summary>
    /// <param name="accountId">The Weixin account identifier.</param>
    /// <param name="identity">The message/media identity used in the file name.</param>
    /// <param name="index">The media item index.</param>
    /// <param name="fileName">The original file name used for extension preservation.</param>
    /// <param name="bytes">The media content.</param>
    /// <param name="cancellationToken">Token used to cancel writing.</param>
    /// <returns>The relative storage key.</returns>
    /// <exception cref="ArgumentException">Thrown when the content is empty.</exception>
    public async Task<string> SaveAsync(
        int accountId,
        string identity,
        int index,
        string fileName,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        if (bytes == null || bytes.Length == 0)
        {
            throw new ArgumentException("媒体内容不能为空。", nameof(bytes));
        }

        var safeAccountId = accountId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var safeIdentity = SanitizeSegment(identity);
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var directory = Path.Combine(_rootPath, safeAccountId);
        Directory.CreateDirectory(directory);
        var storageKey = $"{safeAccountId}/{safeIdentity}-{index}{extension.ToLowerInvariant()}";
        var fullPath = GetFullPath(storageKey);
        await using var stream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);
        await stream.WriteAsync(bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
        return storageKey;
    }

    /// <summary>
    /// Resolves an existing storage key to a file path without allowing path traversal.
    /// </summary>
    /// <param name="storageKey">The relative storage key.</param>
    /// <param name="fullPath">Receives the resolved existing file path.</param>
    /// <returns><see langword="true"/> when the key is valid and the file exists.</returns>
    public bool TryGetFile(
        string storageKey,
        out string fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return false;
        }

        try
        {
            fullPath = GetFullPath(storageKey);
            return File.Exists(fullPath);
        }
        catch
        {
            fullPath = null;
            return false;
        }
    }

    /// <summary>
    /// Resolves a relative media storage key under the configured root and rejects traversal.
    /// </summary>
    /// <param name="storageKey">The normalized relative storage key.</param>
    /// <returns>The absolute file path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the key escapes the storage root.</exception>
    private string GetFullPath(string storageKey)
    {
        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        var root = Path.GetFullPath(_rootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("非法的微信媒体存储路径。");
        }

        return fullPath;
    }

    /// <summary>
    /// Converts a storage-path segment into a safe alphanumeric, dash, or underscore name.
    /// </summary>
    /// <param name="value">The requested segment.</param>
    /// <returns>The sanitized segment, or a generated identifier for blank input.</returns>
    private static string SanitizeSegment(string value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value.Trim();
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] is not '-' and not '_')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }
}
