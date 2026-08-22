/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxWorkspacePaths.cs
    文件功能描述：Sandbox 持久化工作区路径约束

    创建标识：Senparc - 20260822

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxWorkspacePaths
{
    public static string NormalizeRelativePath(string? value, bool allowEmpty = false)
    {
        var normalized = (value ?? string.Empty).Trim().Replace('\\', '/');
        if (normalized.Length == 0)
        {
            if (allowEmpty)
            {
                return string.Empty;
            }

            throw new InvalidOperationException("工作区相对路径不能为空。");
        }

        if (normalized.StartsWith('/')
            || Path.IsPathRooted(normalized)
            || normalized.Contains('\0')
            || normalized.Contains(':'))
        {
            throw new InvalidOperationException("工作区路径必须是相对路径。");
        }

        var segments = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(z => z is "." or ".."))
        {
            throw new InvalidOperationException("工作区路径不允许包含 . 或 ..。");
        }

        foreach (var segment in segments)
        {
            if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("工作区路径包含无效文件名字符。");
            }
        }

        return string.Join('/', segments);
    }

    public static string GetSessionWorkspacePath(string workspaceRoot, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            throw new ArgumentException("工作区根目录不能为空。", nameof(workspaceRoot));
        }

        if (string.IsNullOrWhiteSpace(sessionId)
            || sessionId != Path.GetFileName(sessionId)
            || sessionId.Contains(Path.DirectorySeparatorChar)
            || sessionId.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new InvalidOperationException("SessionId 不是有效的工作区标识。");
        }

        var root = Path.GetFullPath(workspaceRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, sessionId));
        if (!path.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("工作区路径越界。");
        }

        return path;
    }

    public static string CombineHostPath(string workspacePath, string relativePath)
    {
        var relative = NormalizeRelativePath(relativePath);
        var root = Path.GetFullPath(workspacePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("工作区路径越界。");
        }

        return path;
    }

    public static string CombineContainerPath(string mountPath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(mountPath) || !mountPath.StartsWith('/'))
        {
            throw new InvalidOperationException("容器工作区挂载路径配置无效。");
        }

        var relative = NormalizeRelativePath(relativePath, allowEmpty: true);
        return relative.Length == 0
            ? mountPath.TrimEnd('/')
            : $"{mountPath.TrimEnd('/')}/{relative}";
    }
}
