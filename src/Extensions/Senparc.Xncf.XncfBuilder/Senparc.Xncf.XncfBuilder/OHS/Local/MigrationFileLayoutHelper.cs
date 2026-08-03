/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：MigrationFileLayoutHelper.cs
    文件功能描述：实现 MigrationFileLayoutHelper 相关功能。


    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

----------------------------------------------------------------*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    internal sealed class MigrationSnapshotAlignmentResult
    {
        public bool SnapshotFound { get; init; }

        public string OriginalPath { get; init; }

        public string SnapshotPath { get; init; }

        public bool NamespaceChanged { get; init; }

        public IReadOnlyList<string> RemovedDuplicateFiles { get; init; } = Array.Empty<string>();

        public bool Moved => SnapshotFound && !MigrationFileLayoutHelper.PathsEqual(OriginalPath, SnapshotPath);
    }

    internal sealed record GeneratedMigrationFiles(string MigrationFile, string DesignerFile);

    /// <summary>
    /// 统一 Add-Migration 生成文件布局：migration、Designer 和 snapshot
    /// 均位于 Domain/Migrations/{DatabaseType}，与 PromptRange 模块保持一致。
    /// </summary>
    internal static class MigrationFileLayoutHelper
    {
        private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".vs",
            "bin",
            "Generated",
            "obj"
        };

        internal static string GetProjectDirectory(string path, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException($"{parameterName} 不能为空。");
            }

            var fullPath = Path.GetFullPath(path);
            if (string.Equals(Path.GetExtension(fullPath), ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                fullPath = Path.GetDirectoryName(fullPath)
                    ?? throw new InvalidOperationException($"无法获取 {parameterName} 所在目录：{path}");
            }

            if (!Directory.Exists(fullPath))
            {
                throw new DirectoryNotFoundException($"{parameterName} 不存在：{fullPath}");
            }

            return Path.TrimEndingDirectorySeparator(fullPath);
        }

        internal static string GetMigrationDirectory(string projectDirectory, string databaseType)
        {
            return Path.Combine(projectDirectory, "Domain", "Migrations", databaseType);
        }

        internal static string GetOutputDirectoryArgument(string projectDirectory, string migrationDirectory)
        {
            var projectFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(projectDirectory));
            var migrationFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(migrationDirectory));
            var relativePath = Path.GetRelativePath(projectFullPath, migrationFullPath);

            if (Path.IsPathRooted(relativePath)
                || string.Equals(relativePath, "..", StringComparison.Ordinal)
                || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"迁移目录必须位于目标项目内：{migrationFullPath}");
            }

            return relativePath;
        }

        internal static string GetExpectedNamespace(string projectDirectory, string databaseType)
        {
            var projectFiles = Directory.GetFiles(projectDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
            if (projectFiles.Length != 1)
            {
                throw new InvalidOperationException(
                    $"目标项目目录必须且只能包含一个 .csproj 文件：{projectDirectory}（当前 {projectFiles.Length} 个）。");
            }

            var projectDocument = XDocument.Load(projectFiles[0], LoadOptions.PreserveWhitespace);
            var rootNamespace = GetProjectProperty(projectDocument, "RootNamespace")
                                ?? GetProjectProperty(projectDocument, "AssemblyName")
                                ?? Path.GetFileNameWithoutExtension(projectFiles[0]);

            if (string.IsNullOrWhiteSpace(rootNamespace) || rootNamespace.Contains("$(", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"无法从项目文件获取有效的 RootNamespace：{projectFiles[0]}");
            }

            return $"{rootNamespace}.Domain.Migrations.{databaseType}";
        }

        internal static IReadOnlyCollection<string> CaptureMigrationFiles(string migrationDirectory)
        {
            if (!Directory.Exists(migrationDirectory))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(migrationDirectory, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFullPath)
                .ToArray();
        }

        internal static GeneratedMigrationFiles VerifyGeneratedMigrationFiles(
            string migrationDirectory,
            IReadOnlyCollection<string> filesBefore)
        {
            var pathComparer = OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            var previousFiles = new HashSet<string>(filesBefore.Select(Path.GetFullPath), pathComparer);
            var generatedFiles = Directory.GetFiles(migrationDirectory, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFullPath)
                .Where(path => !previousFiles.Contains(path))
                .Where(path => !path.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
                .ToArray();

            var designerFiles = generatedFiles
                .Where(path => path.EndsWith(".Designer.cs", StringComparison.Ordinal))
                .ToArray();
            var migrationFiles = generatedFiles
                .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal))
                .ToArray();

            if (migrationFiles.Length != 1 || designerFiles.Length != 1)
            {
                throw new InvalidOperationException(
                    $"迁移命令应在 {migrationDirectory} 中各生成一个 migration 和 Designer 文件，"
                    + $"实际为 migration {migrationFiles.Length} 个、Designer {designerFiles.Length} 个。");
            }

            var expectedDesignerPath = Path.Combine(
                migrationDirectory,
                Path.GetFileNameWithoutExtension(migrationFiles[0]) + ".Designer.cs");
            if (!PathsEqual(expectedDesignerPath, designerFiles[0]))
            {
                throw new InvalidOperationException(
                    $"migration 与 Designer 文件名不匹配：{migrationFiles[0]}、{designerFiles[0]}");
            }

            return new GeneratedMigrationFiles(migrationFiles[0], designerFiles[0]);
        }

        internal static MigrationSnapshotAlignmentResult AlignSnapshot(
            string projectDirectory,
            string migrationDirectory,
            string dbContextName,
            string expectedNamespace)
        {
            Directory.CreateDirectory(migrationDirectory);

            var snapshots = EnumerateSnapshotFiles(projectDirectory)
                .Select(TryReadSnapshot)
                .Where(snapshot => snapshot != null
                                   && string.Equals(snapshot.DbContextName, dbContextName, StringComparison.Ordinal))
                .ToList();

            if (snapshots.Count == 0)
            {
                return new MigrationSnapshotAlignmentResult { SnapshotFound = false };
            }

            var snapshotClassNames = snapshots
                .Select(snapshot => snapshot.ClassName)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (snapshotClassNames.Length != 1)
            {
                throw new InvalidOperationException(
                    $"DbContext {dbContextName} 存在多个不同的 snapshot 类型：{string.Join(", ", snapshotClassNames)}。");
            }

            var snapshotClassName = snapshotClassNames[0];
            var expectedFileName = snapshotClassName + ".cs";
            var expectedPath = Path.Combine(migrationDirectory, expectedFileName);

            var selectedSnapshot = snapshots.FirstOrDefault(snapshot => PathsEqual(snapshot.Path, expectedPath));
            if (selectedSnapshot == null)
            {
                var fileNameMatches = snapshots
                    .Where(snapshot => string.Equals(Path.GetFileName(snapshot.Path), expectedFileName, StringComparison.Ordinal))
                    .ToArray();

                if (fileNameMatches.Length == 1)
                {
                    selectedSnapshot = fileNameMatches[0];
                }
                else if (snapshots.Count == 1)
                {
                    selectedSnapshot = snapshots[0];
                }
                else
                {
                    throw new InvalidOperationException(
                        $"DbContext {dbContextName} 存在多个无法自动判定的 snapshot 文件：{string.Join(", ", snapshots.Select(z => z.Path))}。");
                }
            }

            if (!PathsEqual(selectedSnapshot.Path, expectedPath)
                && File.Exists(expectedPath)
                && snapshots.All(snapshot => !PathsEqual(snapshot.Path, expectedPath)))
            {
                throw new IOException($"目标 snapshot 路径已被其他文件占用：{expectedPath}");
            }

            var originalPath = selectedSnapshot.Path;
            if (!PathsEqual(originalPath, expectedPath))
            {
                File.Move(originalPath, expectedPath);
            }

            var removedFiles = new List<string>();
            foreach (var duplicate in snapshots.Where(snapshot => !PathsEqual(snapshot.Path, originalPath)))
            {
                var duplicatePath = PathsEqual(duplicate.Path, expectedPath) ? originalPath : duplicate.Path;
                if (!PathsEqual(duplicatePath, expectedPath) && File.Exists(duplicatePath))
                {
                    File.Delete(duplicatePath);
                    removedFiles.Add(duplicatePath);
                }
            }

            var alignedSnapshot = TryReadSnapshot(expectedPath)
                ?? throw new InvalidOperationException($"无法重新读取 snapshot：{expectedPath}");
            var namespaceChanged = !string.Equals(alignedSnapshot.Namespace, expectedNamespace, StringComparison.Ordinal);
            if (namespaceChanged)
            {
                RewriteNamespace(alignedSnapshot, expectedNamespace);
            }

            var remainingSnapshots = EnumerateSnapshotFiles(projectDirectory)
                .Select(TryReadSnapshot)
                .Where(snapshot => snapshot != null
                                   && string.Equals(snapshot.DbContextName, dbContextName, StringComparison.Ordinal))
                .ToArray();

            if (remainingSnapshots.Length != 1 || !PathsEqual(remainingSnapshots[0].Path, expectedPath))
            {
                throw new InvalidOperationException(
                    $"DbContext {dbContextName} 的 snapshot 未能唯一归位到迁移目录：{expectedPath}");
            }

            return new MigrationSnapshotAlignmentResult
            {
                SnapshotFound = true,
                OriginalPath = originalPath,
                SnapshotPath = expectedPath,
                NamespaceChanged = namespaceChanged,
                RemovedDuplicateFiles = removedFiles
            };
        }

        internal static bool PathsEqual(string first, string second)
        {
            if (first == null || second == null)
            {
                return first == second;
            }

            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), comparison);
        }

        private static string GetProjectProperty(XDocument projectDocument, string propertyName)
        {
            return projectDocument
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.Ordinal))
                .Select(element => element.Value?.Trim())
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && !value.Contains("$(", StringComparison.Ordinal));
        }

        private static IEnumerable<string> EnumerateSnapshotFiles(string projectDirectory)
        {
            return Directory
                .EnumerateFiles(projectDirectory, "*ModelSnapshot.cs", SearchOption.AllDirectories)
                .Where(path => !ContainsIgnoredDirectory(projectDirectory, path));
        }

        private static bool ContainsIgnoredDirectory(string projectDirectory, string path)
        {
            var relativePath = Path.GetRelativePath(projectDirectory, path);
            return relativePath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                .TakeWhile(segment => !string.Equals(segment, Path.GetFileName(path), StringComparison.Ordinal))
                .Any(IgnoredDirectoryNames.Contains);
        }

        private static SnapshotSourceInfo TryReadSnapshot(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var hasUtf8Bom = bytes.Length >= 3
                             && bytes[0] == 0xEF
                             && bytes[1] == 0xBB
                             && bytes[2] == 0xBF;
            var content = Encoding.UTF8.GetString(bytes, hasUtf8Bom ? 3 : 0, bytes.Length - (hasUtf8Bom ? 3 : 0));
            var syntaxRoot = CSharpSyntaxTree.ParseText(content, path: path).GetRoot();

            foreach (var classDeclaration in syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (classDeclaration.BaseList?.Types.Any(baseType =>
                        string.Equals(GetSimpleName(baseType.Type), nameof(Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot), StringComparison.Ordinal)) != true)
                {
                    continue;
                }

                var dbContextAttribute = classDeclaration.AttributeLists
                    .SelectMany(list => list.Attributes)
                    .FirstOrDefault(attribute =>
                    {
                        var attributeName = GetSimpleName(attribute.Name);
                        return string.Equals(attributeName, "DbContext", StringComparison.Ordinal)
                               || string.Equals(attributeName, "DbContextAttribute", StringComparison.Ordinal);
                    });
                var dbContextType = dbContextAttribute?.ArgumentList?.Arguments
                    .Select(argument => argument.Expression)
                    .OfType<TypeOfExpressionSyntax>()
                    .Select(typeOfExpression => GetSimpleName(typeOfExpression.Type))
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(dbContextType))
                {
                    continue;
                }

                var namespaceDeclaration = classDeclaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
                if (namespaceDeclaration == null)
                {
                    throw new InvalidOperationException($"snapshot 缺少命名空间：{path}");
                }

                return new SnapshotSourceInfo(
                    path,
                    hasUtf8Bom,
                    syntaxRoot,
                    namespaceDeclaration,
                    classDeclaration.Identifier.ValueText,
                    dbContextType,
                    namespaceDeclaration.Name.ToString());
            }

            return null;
        }

        private static string GetSimpleName(SyntaxNode syntaxNode)
        {
            return syntaxNode.DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .LastOrDefault()?.Identifier.ValueText
                   ?? syntaxNode.ToString().Split('.').Last();
        }

        private static void RewriteNamespace(SnapshotSourceInfo snapshot, string expectedNamespace)
        {
            BaseNamespaceDeclarationSyntax replacement = snapshot.NamespaceDeclaration switch
            {
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.WithName(
                    SyntaxFactory.ParseName(expectedNamespace).WithTriviaFrom(namespaceDeclaration.Name)),
                FileScopedNamespaceDeclarationSyntax fileScopedNamespace => fileScopedNamespace.WithName(
                    SyntaxFactory.ParseName(expectedNamespace).WithTriviaFrom(fileScopedNamespace.Name)),
                _ => throw new InvalidOperationException($"无法识别 snapshot 命名空间语法：{snapshot.Path}")
            };

            var newRoot = snapshot.SyntaxRoot.ReplaceNode(snapshot.NamespaceDeclaration, replacement);
            File.WriteAllText(snapshot.Path, newRoot.ToFullString(), new UTF8Encoding(snapshot.HasUtf8Bom));
        }

        private sealed record SnapshotSourceInfo(
            string Path,
            bool HasUtf8Bom,
            SyntaxNode SyntaxRoot,
            BaseNamespaceDeclarationSyntax NamespaceDeclaration,
            string ClassName,
            string DbContextName,
            string Namespace);
    }
}
