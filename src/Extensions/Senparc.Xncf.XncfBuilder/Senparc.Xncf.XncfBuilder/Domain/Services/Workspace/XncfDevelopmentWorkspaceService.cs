/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentWorkspaceService.cs
    文件功能描述：创建不含密钥和构建产物的 XNCF 开发工作区快照

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

    修改标识：Senparc - 20260822
    修改描述：v0.41.0 优化 XncfBuilder 预览任务与工作区服务

----------------------------------------------------------------*/

using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Workspace
{
    internal sealed record XncfDevelopmentWorkspaceSnapshot(
        string SourceRootPath,
        string WorkspaceRootPath,
        string WorkspaceSolutionFilePath);

    internal sealed record XncfDevelopmentWorkspaceDiff(
        string Summary,
        string WorkspaceModuleFingerprint);

    /// <summary>
    /// Copies a source tree before any template generation or AI edit. The target checkout is
    /// read-only to this service by design. A sanitized copy is also the only tree handed to a
    /// sandbox; secrets, stateful data and symlinks never cross that boundary.
    /// </summary>
    internal static class XncfDevelopmentWorkspaceService
    {
        private const long MaxSnapshotBytes = 768L * 1024 * 1024;

        private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".idea", ".vs", ".vscode", "bin", "obj", "node_modules", "packages", "App_Data"
        };

        private static readonly Regex ProjectPathRegex = new(
            "\\\"(?<path>[^\\\"\\r\\n]+\\.csproj)\\\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static async Task<XncfDevelopmentWorkspaceSnapshot> CreateSnapshotAsync(
            string sourceSolutionFilePath,
            string jobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceSolutionFilePath)
                || !string.Equals(Path.GetExtension(sourceSolutionFilePath), ".sln", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(sourceSolutionFilePath))
            {
                throw new FileNotFoundException("未找到需要快照的解决方案文件。", sourceSolutionFilePath);
            }
            if (string.IsNullOrWhiteSpace(jobId) || jobId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("开发任务 ID 无效。", nameof(jobId));
            }

            var fullSolutionPath = Path.GetFullPath(sourceSolutionFilePath);
            var sourceRoot = FindSourceRoot(fullSolutionPath);
            var workspaceRoot = Path.Combine(Path.GetTempPath(), "Senparc.Ncf", "XncfDevelopment", jobId, "workspace");
            if (Directory.Exists(workspaceRoot))
            {
                throw new InvalidOperationException("该开发任务的隔离工作区已存在，拒绝覆盖。请新建任务或先显式丢弃旧任务。");
            }

            var solutionRelativePath = Path.GetRelativePath(sourceRoot, fullSolutionPath);
            if (solutionRelativePath.StartsWith(".." + Path.DirectorySeparatorChar, GetPathComparison())
                || Path.IsPathRooted(solutionRelativePath))
            {
                throw new InvalidOperationException("解决方案不在已验证的源码根目录内。");
            }

            try
            {
                Directory.CreateDirectory(workspaceRoot);
                await CopySanitizedTreeAsync(sourceRoot, workspaceRoot, cancellationToken).ConfigureAwait(false);
                var workspaceSolution = Path.Combine(workspaceRoot, solutionRelativePath);
                if (!File.Exists(workspaceSolution))
                {
                    throw new InvalidOperationException("隔离工作区未包含解决方案文件。");
                }

                return new XncfDevelopmentWorkspaceSnapshot(sourceRoot, workspaceRoot, workspaceSolution);
            }
            catch
            {
                TryDeleteWorkspace(jobId);
                throw;
            }
        }

        internal static XncfDevelopmentWorkspaceDiff BuildModuleDiff(
            string targetSolutionFilePath,
            string workspaceSolutionFilePath,
            string moduleProjectName,
            bool isNewModule)
        {
            var workspaceModule = XncfWorkspaceFileService.ResolveModuleDirectory(
                workspaceSolutionFilePath,
                moduleProjectName);
            var workspaceFingerprint = XncfPreviewService.ComputeSourceFingerprint(workspaceModule);
            if (isNewModule)
            {
                var createdCount = EnumerateTrackedFiles(workspaceModule).Count();
                return new XncfDevelopmentWorkspaceDiff(
                    $"新模块快照：{createdCount} 个受控文件，模块 SHA-256：{workspaceFingerprint}",
                    workspaceFingerprint);
            }

            var targetModule = XncfWorkspaceFileService.ResolveModuleDirectory(
                targetSolutionFilePath,
                moduleProjectName);
            var sourceFiles = CreateFileHashMap(targetModule);
            var workspaceFiles = CreateFileHashMap(workspaceModule);
            var added = workspaceFiles.Keys.Except(sourceFiles.Keys, StringComparer.OrdinalIgnoreCase).Count();
            var changed = workspaceFiles.Count(item => sourceFiles.TryGetValue(item.Key, out var hash)
                                                  && !string.Equals(hash, item.Value, StringComparison.OrdinalIgnoreCase));
            var removed = sourceFiles.Keys.Except(workspaceFiles.Keys, StringComparer.OrdinalIgnoreCase).Count();
            return new XncfDevelopmentWorkspaceDiff(
                $"模块差异：新增 {added}，修改 {changed}，删除 {removed}（隔离工具不提供删除操作），模块 SHA-256：{workspaceFingerprint}",
                workspaceFingerprint);
        }

        internal static void TryDeleteWorkspace(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId) || jobId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return;
            }

            try
            {
                var jobDirectory = Path.Combine(Path.GetTempPath(), "Senparc.Ncf", "XncfDevelopment", jobId);
                if (Directory.Exists(jobDirectory))
                {
                    Directory.Delete(jobDirectory, recursive: true);
                }
            }
            catch
            {
                // The persistence record remains, so an administrator can diagnose a locked file.
            }
        }

        private static string FindSourceRoot(string solutionFilePath)
        {
            var solutionDirectory = Path.GetDirectoryName(solutionFilePath)
                ?? throw new InvalidOperationException("无法获取解决方案目录。");
            var projectPaths = ResolveProjectClosure(solutionFilePath).ToArray();
            var candidateDirectories = projectPaths
                .Select(Path.GetDirectoryName)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Append(solutionDirectory)
                .Select(Path.GetFullPath)
                .ToArray();
            var root = candidateDirectories.Aggregate(GetCommonAncestor);
            if (string.IsNullOrWhiteSpace(root)
                || string.Equals(root, Path.GetPathRoot(root), GetPathComparison()))
            {
                throw new InvalidOperationException("无法确定安全的源码根目录；项目引用不能跨越文件系统根目录。");
            }

            var repositoryRoot = FindRepositoryRoot(solutionDirectory);
            if (repositoryRoot != null)
            {
                foreach (var projectPath in projectPaths)
                {
                    if (!IsWithin(repositoryRoot, projectPath))
                    {
                        throw new InvalidOperationException("项目引用指向当前仓库外部；为避免复制不受控来源，隔离开发已拒绝该解决方案。");
                    }
                }

                return repositoryRoot;
            }

            return root;
        }

        private static IEnumerable<string> ResolveProjectClosure(string solutionFilePath)
        {
            var solutionDirectory = Path.GetDirectoryName(solutionFilePath)!;
            var pending = new Queue<string>();
            var visited = new HashSet<string>(GetPathComparer());
            foreach (Match match in ProjectPathRegex.Matches(File.ReadAllText(solutionFilePath)))
            {
                var relative = match.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar);
                var projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, relative));
                if (File.Exists(projectPath))
                {
                    pending.Enqueue(projectPath);
                }
            }

            // A lightweight solution created by a user may not list all projects yet. Senparc.Web
            // is required for preview and is a reliable fallback root of the graph.
            var hostProject = Path.Combine(solutionDirectory, "Senparc.Web", "Senparc.Web.csproj");
            if (File.Exists(hostProject))
            {
                pending.Enqueue(hostProject);
            }

            while (pending.Count > 0)
            {
                var projectPath = pending.Dequeue();
                if (!visited.Add(projectPath))
                {
                    continue;
                }

                yield return projectPath;
                var projectDirectory = Path.GetDirectoryName(projectPath)!;
                XDocument project;
                try
                {
                    project = XDocument.Load(projectPath, LoadOptions.None);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"无法读取项目引用：{projectPath}", ex);
                }

                foreach (var include in project.Descendants()
                             .Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
                             .Select(element => element.Attribute("Include")?.Value)
                             .Where(value => !string.IsNullOrWhiteSpace(value) && !value.Contains("$(", StringComparison.Ordinal)))
                {
                    var referencePath = Path.GetFullPath(Path.Combine(
                        projectDirectory,
                        include!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));
                    if (File.Exists(referencePath))
                    {
                        pending.Enqueue(referencePath);
                    }
                }
            }
        }

        private static async Task CopySanitizedTreeAsync(string sourceRoot, string destinationRoot, CancellationToken cancellationToken)
        {
            var pending = new Stack<(string Source, string Relative)>();
            pending.Push((sourceRoot, string.Empty));
            long copiedBytes = 0;

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (sourceDirectory, relativeDirectory) = pending.Pop();
                var info = new DirectoryInfo(sourceDirectory);
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new UnauthorizedAccessException("隔离工作区拒绝复制包含符号链接的源码目录。");
                }

                foreach (var directory in Directory.EnumerateDirectories(sourceDirectory))
                {
                    var child = new DirectoryInfo(directory);
                    if (ExcludedDirectoryNames.Contains(child.Name))
                    {
                        continue;
                    }
                    if (child.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        throw new UnauthorizedAccessException($"隔离工作区拒绝复制符号链接目录：{child.FullName}");
                    }

                    pending.Push((child.FullName, Path.Combine(relativeDirectory, child.Name)));
                }

                foreach (var filePath in Directory.EnumerateFiles(sourceDirectory))
                {
                    var file = new FileInfo(filePath);
                    if (file.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        throw new UnauthorizedAccessException($"隔离工作区拒绝复制符号链接文件：{file.FullName}");
                    }
                    if (ShouldExcludeFile(file.Name))
                    {
                        continue;
                    }

                    copiedBytes += file.Length;
                    if (copiedBytes > MaxSnapshotBytes)
                    {
                        throw new InvalidOperationException($"源码快照超过 {MaxSnapshotBytes / 1024 / 1024} MB 安全上限，请缩小解决方案或配置专用构建镜像。");
                    }

                    var destinationDirectory = Path.Combine(destinationRoot, relativeDirectory);
                    Directory.CreateDirectory(destinationDirectory);
                    var destinationFile = Path.Combine(destinationDirectory, file.Name);
                    await using var source = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await using var destination = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    await source.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static bool ShouldExcludeFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return fileName.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)
                   || fileName.StartsWith(".env", StringComparison.OrdinalIgnoreCase)
                   || fileName.Equals("nuget.config", StringComparison.OrdinalIgnoreCase)
                   || fileName.Equals("SenparcConfig.config", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".key", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".pem", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".snk", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> CreateFileHashMap(string root)
        {
            return EnumerateTrackedFiles(root).ToDictionary(
                path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'),
                XncfWorkspaceFileService.ComputeFileSha256,
                StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> EnumerateTrackedFiles(string root)
        {
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    var info = new DirectoryInfo(directory);
                    if (!ExcludedDirectoryNames.Contains(info.Name) && !info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        pending.Push(info.FullName);
                    }
                }
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    var info = new FileInfo(file);
                    if (!info.Attributes.HasFlag(FileAttributes.ReparsePoint) && !ShouldExcludeFile(info.Name))
                    {
                        yield return info.FullName;
                    }
                }
            }
        }

        private static string FindRepositoryRoot(string directory)
        {
            for (var current = new DirectoryInfo(directory); current != null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")))
                {
                    return current.FullName;
                }
            }
            return null;
        }

        private static string GetCommonAncestor(string first, string second)
        {
            var left = Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var right = Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            while (!IsWithin(left, right))
            {
                var parent = Directory.GetParent(left)?.FullName;
                if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, left, GetPathComparison()))
                {
                    return Path.GetPathRoot(left)!;
                }
                left = parent;
            }
            return left;
        }

        private static bool IsWithin(string root, string path)
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedPath = Path.GetFullPath(path);
            return string.Equals(normalizedRoot, normalizedPath, GetPathComparison())
                   || normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, GetPathComparison());
        }

        private static StringComparer GetPathComparer() =>
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private static StringComparison GetPathComparison() =>
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }
}
