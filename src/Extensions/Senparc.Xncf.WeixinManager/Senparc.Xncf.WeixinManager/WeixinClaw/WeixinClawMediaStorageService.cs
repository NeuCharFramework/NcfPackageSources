using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawMediaStorageService
{
    private readonly string _rootPath;

    public WeixinClawMediaStorageService(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "WeixinClawMedia");
    }

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
