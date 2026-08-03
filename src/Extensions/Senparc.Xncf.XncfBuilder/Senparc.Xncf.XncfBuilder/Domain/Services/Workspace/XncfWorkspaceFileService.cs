/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfWorkspaceFileService.cs
    文件功能描述：约束 XNCF 工作区文件访问并提供原子写入和并发指纹校验


    创建标识：Senparc - 20260802

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Workspace
{
    internal static class XncfWorkspaceFileService
    {
        private const int MaxTextFileBytes = 4 * 1024 * 1024;

        private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".idea",
            ".vs",
            ".vscode",
            "bin",
            "node_modules",
            "obj",
            "packages"
        };

        internal static string ResolveModuleDirectory(string solutionFilePath, string moduleName)
        {
            if (string.IsNullOrWhiteSpace(solutionFilePath)
                || !string.Equals(Path.GetExtension(solutionFilePath), ".sln", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(solutionFilePath))
            {
                throw new FileNotFoundException("未找到 XNCF 工作区解决方案文件。", solutionFilePath);
            }

            ValidateModuleName(moduleName);

            var solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionFilePath))
                ?? throw new InvalidOperationException("无法获取 XNCF 工作区解决方案目录。");
            var directModuleDirectory = Path.Combine(solutionDirectory, moduleName);
            var directProjectFile = Path.Combine(directModuleDirectory, $"{moduleName}.csproj");
            if (File.Exists(directProjectFile))
            {
                EnsureNoSymbolicLink(solutionDirectory, directModuleDirectory);
                return Path.GetFullPath(directModuleDirectory);
            }

            var matches = EnumerateModuleDirectories(solutionDirectory, moduleName)
                .Distinct(GetPathComparer())
                .Take(2)
                .ToArray();

            return matches.Length switch
            {
                0 => throw new DirectoryNotFoundException($"未找到模块 {moduleName} 的项目目录；模块名称必须完整匹配，例如 Senparc.Xncf.XncfBuilder。"),
                1 => matches[0],
                _ => throw new InvalidOperationException($"找到多个名为 {moduleName} 的 XNCF 项目，请使用无歧义的解决方案工作区。")
            };
        }

        internal static string ResolveFilePath(string moduleDirectory, string relativeFilePath)
        {
            if (string.IsNullOrWhiteSpace(relativeFilePath))
            {
                throw new ArgumentException("必须提供模块内的相对文件路径。", nameof(relativeFilePath));
            }

            var normalizedRelativePath = relativeFilePath
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(normalizedRelativePath)
                || (normalizedRelativePath.Length >= 2
                    && char.IsLetter(normalizedRelativePath[0])
                    && normalizedRelativePath[1] == ':'))
            {
                throw new ArgumentException("只允许访问模块目录内的相对文件路径。", nameof(relativeFilePath));
            }

            var moduleRoot = Path.GetFullPath(moduleDirectory);
            var fullFilePath = Path.GetFullPath(Path.Combine(moduleRoot, normalizedRelativePath));
            var modulePrefix = moduleRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!fullFilePath.StartsWith(modulePrefix, GetPathComparison()))
            {
                throw new UnauthorizedAccessException("文件路径超出了所选 XNCF 模块目录。");
            }

            EnsureNoSymbolicLink(moduleRoot, fullFilePath);
            if (Directory.Exists(fullFilePath))
            {
                throw new IOException("目标路径是目录，不能作为代码文件读取或写入。");
            }

            return fullFilePath;
        }

        internal static async Task<XncfWorkspaceReadResult> ReadTextAsync(
            string moduleDirectory,
            string relativeFilePath,
            CancellationToken cancellationToken = default)
        {
            var fullFilePath = ResolveFilePath(moduleDirectory, relativeFilePath);
            if (!File.Exists(fullFilePath))
            {
                throw new FileNotFoundException("模块文件不存在。", relativeFilePath);
            }

            if (new FileInfo(fullFilePath).Length > MaxTextFileBytes)
            {
                throw new InvalidOperationException($"文件超过允许读取的 {MaxTextFileBytes / 1024 / 1024} MB 文本文件上限。");
            }

            var bytes = await File.ReadAllBytesAsync(fullFilePath, cancellationToken).ConfigureAwait(false);
            if (bytes.Length > MaxTextFileBytes)
            {
                throw new InvalidOperationException($"文件超过允许读取的 {MaxTextFileBytes / 1024 / 1024} MB 文本文件上限。");
            }

            await using var contentStream = new MemoryStream(bytes, writable: false);
            using var reader = new StreamReader(
                contentStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            return new XncfWorkspaceReadResult(fullFilePath, content, ComputeSha256(bytes));
        }

        internal static async Task<XncfWorkspaceWriteResult> WriteTextAtomicAsync(
            string moduleDirectory,
            string relativeFilePath,
            string content,
            string expectedSha256 = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            if (content.IndexOf('\0') >= 0)
            {
                throw new ArgumentException("代码文件内容不能包含 NUL 字符。", nameof(content));
            }

            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
            if (bytes.Length > MaxTextFileBytes)
            {
                throw new InvalidOperationException($"文件超过允许写入的 {MaxTextFileBytes / 1024 / 1024} MB 文本文件上限。");
            }

            var fullFilePath = ResolveFilePath(moduleDirectory, relativeFilePath);
            var fileExists = File.Exists(fullFilePath);
            var previousSha256 = fileExists ? ComputeFileSha256(fullFilePath) : null;
            if (!string.IsNullOrWhiteSpace(expectedSha256)
                && !string.Equals(expectedSha256.Trim(), previousSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("文件在读取后已被其他操作修改，SHA-256 指纹不匹配；请重新读取后再提交。");
            }

            var directoryPath = Path.GetDirectoryName(fullFilePath)
                ?? throw new InvalidOperationException("无法获取目标文件目录。");
            Directory.CreateDirectory(directoryPath);
            EnsureNoSymbolicLink(Path.GetFullPath(moduleDirectory), directoryPath);

            var temporaryFilePath = Path.Combine(
                directoryPath,
                $".{Path.GetFileName(fullFilePath)}.{Guid.NewGuid():N}.xncfbuilder.tmp");
            try
            {
                await using (var stream = new FileStream(
                    temporaryFilePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temporaryFilePath, fullFilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryFilePath))
                {
                    File.Delete(temporaryFilePath);
                }
            }

            return new XncfWorkspaceWriteResult(
                fullFilePath,
                !fileExists,
                previousSha256,
                ComputeSha256(bytes));
        }

        internal static string ComputeFileSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        private static string ComputeSha256(byte[] bytes)
        {
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }

        private static IEnumerable<string> EnumerateModuleDirectories(string solutionDirectory, string moduleName)
        {
            var pending = new Stack<string>();
            pending.Push(solutionDirectory);

            while (pending.Count > 0)
            {
                var currentDirectory = pending.Pop();
                foreach (var directory in Directory.EnumerateDirectories(currentDirectory))
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    if (ExcludedDirectoryNames.Contains(directoryInfo.Name)
                        || directoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        continue;
                    }

                    if (string.Equals(directoryInfo.Name, moduleName, StringComparison.OrdinalIgnoreCase)
                        && File.Exists(Path.Combine(directoryInfo.FullName, $"{moduleName}.csproj")))
                    {
                        yield return directoryInfo.FullName;
                    }

                    pending.Push(directoryInfo.FullName);
                }
            }
        }

        private static void ValidateModuleName(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName)
                || moduleName is "." or ".."
                || !string.Equals(moduleName, Path.GetFileName(moduleName), StringComparison.Ordinal)
                || moduleName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || moduleName.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')))
            {
                throw new ArgumentException("XNCF 项目名称无效，必须提供完整项目名称，例如 Senparc.Xncf.Sample。", nameof(moduleName));
            }
        }

        private static void EnsureNoSymbolicLink(string rootPath, string targetPath)
        {
            var root = Path.GetFullPath(rootPath);
            var target = Path.GetFullPath(targetPath);
            var relativePath = Path.GetRelativePath(root, target);
            var currentPath = root;

            foreach (var segment in relativePath.Split(
                         new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                currentPath = Path.Combine(currentPath, segment);
                if ((Directory.Exists(currentPath) || File.Exists(currentPath))
                    && File.GetAttributes(currentPath).HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new UnauthorizedAccessException("不允许通过符号链接访问 XNCF 工作区外部文件。");
                }
            }
        }

        private static StringComparer GetPathComparer()
        {
            return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
        }

        private static StringComparison GetPathComparison()
        {
            return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }
    }

    internal sealed record XncfWorkspaceReadResult(
        string FullFilePath,
        string Content,
        string Sha256);

    internal sealed record XncfWorkspaceWriteResult(
        string FullFilePath,
        bool IsNewFile,
        string PreviousSha256,
        string Sha256);
}
