using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.FileManager.Domain.Services;

/// <summary>
/// Parses the browser-supplied relative path of a folder upload without ever
/// turning it into a physical path. It is only used to build NcfFolder records.
/// </summary>
public static class NcfFolderUploadPath
{
    public const int MaxDepth = 20;
    public const int MaxRelativePathLength = 2048;

    public static IReadOnlyList<string> GetFolderSegments(string relativePath, string uploadedFileName)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Array.Empty<string>();
        }

        if (relativePath.Length > MaxRelativePathLength)
        {
            throw new ArgumentException($"上传相对路径不能超过 {MaxRelativePathLength} 个字符。", nameof(relativePath));
        }

        var normalizedPath = relativePath.Replace('\\', '/');
        if (normalizedPath.StartsWith("/", StringComparison.Ordinal) || normalizedPath.Contains(":/", StringComparison.Ordinal))
        {
            throw new ArgumentException("上传路径必须是相对于所选文件夹的路径。", nameof(relativePath));
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || segments.Length > MaxDepth + 1)
        {
            throw new ArgumentException($"上传目录层级必须介于 1 到 {MaxDepth} 层之间。", nameof(relativePath));
        }

        if (segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("上传路径不能包含相对路径标记。", nameof(relativePath));
        }

        var fileName = Path.GetFileName((uploadedFileName ?? string.Empty).Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName) || !string.Equals(segments[^1], fileName, StringComparison.Ordinal))
        {
            throw new ArgumentException("上传路径与文件名不一致。", nameof(relativePath));
        }

        return segments.Take(segments.Length - 1).ToArray();
    }
}
