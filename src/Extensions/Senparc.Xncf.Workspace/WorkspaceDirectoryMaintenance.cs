/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceDirectoryMaintenance.cs
    文件功能描述：工作区目录用量统计与内容重置


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：统计工作区文件数量与容量，并可将目录恢复到新建布局

----------------------------------------------------------------*/

using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace;

internal static class WorkspaceDirectoryMaintenance
{
    private static readonly string[] InitialDirectories = ["input", "output", "logs", ".workspace"];
    private const string ManifestName = ".workspace.json";

    public static WorkspaceUsageStatistics Measure(
        string workspaceRoot,
        long quotaBytes,
        CancellationToken cancellationToken = default)
    {
        EnsureRealDirectory(workspaceRoot);
        var fileCount = 0;
        var directoryCount = 0;
        var usedBytes = 0L;
        var pending = new Stack<string>();
        pending.Push(workspaceRoot);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    directoryCount++;
                    pending.Push(path);
                    continue;
                }

                if (IsRootManifest(workspaceRoot, path))
                {
                    continue;
                }

                fileCount = checked(fileCount + 1);
                usedBytes = checked(usedBytes + new FileInfo(path).Length);
            }
        }

        return new WorkspaceUsageStatistics(fileCount, directoryCount, usedBytes, quotaBytes);
    }

    public static void ResetToInitialLayout(
        string workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        EnsureRealDirectory(workspaceRoot);
        foreach (var entry in Directory.EnumerateFileSystemEntries(workspaceRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsRootManifest(workspaceRoot, entry))
            {
                continue;
            }

            DeleteTree(entry, cancellationToken);
        }

        foreach (var directory in InitialDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.Combine(workspaceRoot, directory));
        }
    }

    private static void DeleteTree(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var attributes = File.GetAttributes(path);
        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                Directory.Delete(path, recursive: false);
            }
            else
            {
                File.Delete(path);
            }

            return;
        }

        if (!attributes.HasFlag(FileAttributes.Directory))
        {
            File.Delete(path);
            return;
        }

        foreach (var child in Directory.EnumerateFileSystemEntries(path))
        {
            DeleteTree(child, cancellationToken);
        }

        Directory.Delete(path, recursive: false);
    }

    private static bool IsRootManifest(string workspaceRoot, string path)
    {
        var parent = Path.GetDirectoryName(path);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return parent != null
               && string.Equals(parent, workspaceRoot, comparison)
               && string.Equals(Path.GetFileName(path), ManifestName, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureRealDirectory(string workspaceRoot)
    {
        if (!Directory.Exists(workspaceRoot))
        {
            throw new DirectoryNotFoundException("Workspace does not exist.");
        }

        if (File.GetAttributes(workspaceRoot).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("Workspace root cannot be a symbolic link.");
        }
    }
}
