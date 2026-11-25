/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceDirectoryMaintenance.cs
    File description: Workspace directory usage statistics and content reset

    Created by: Senparc - 20261109

    Modified by: Senparc - 20261109
    Change description: count Workspace files and capacity, and restore directories to their initial layout

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace;

internal static class WorkspaceDirectoryMaintenance
{
    private static readonly string[] InitialDirectories = ["input", "output", "logs", ".workspace"];
    private const string ManifestName = ".workspace.json";

    /// <summary>
    /// Measures non-manifest files and directories beneath a real workspace root.
    /// </summary>
    /// <param name="workspaceRoot">The physical workspace root.</param>
    /// <param name="quotaBytes">The quota included in the returned statistics.</param>
    /// <param name="cancellationToken">Token used to cancel traversal.</param>
    /// <returns>File count, directory count, used bytes, and quota information.</returns>
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

    /// <summary>
    /// Deletes workspace content except the root manifest and recreates required directories.
    /// </summary>
    /// <param name="workspaceRoot">The physical workspace root.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
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

    /// <summary>
    /// Recursively deletes a file-system entry while refusing to follow reparse points.
    /// </summary>
    /// <param name="path">The file or directory to delete.</param>
    /// <param name="cancellationToken">Token used to cancel traversal.</param>
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

    /// <summary>
    /// Determines whether a path is the protected manifest directly under the workspace root.
    /// </summary>
    /// <param name="workspaceRoot">The workspace root.</param>
    /// <param name="path">The candidate path.</param>
    /// <returns><see langword="true"/> when the path is the root manifest.</returns>
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

    /// <summary>
    /// Verifies that the workspace root exists and is not a symbolic link.
    /// </summary>
    /// <param name="workspaceRoot">The physical workspace root.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown when the root does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the root is a reparse point.</exception>
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
