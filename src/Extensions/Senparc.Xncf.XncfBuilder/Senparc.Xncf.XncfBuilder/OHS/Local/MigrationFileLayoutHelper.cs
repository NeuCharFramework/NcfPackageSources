/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MigrationFileLayoutHelper.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

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
    /// Locates and aligns EF migration files and model snapshots without modifying
    /// generated migration contents beyond required namespace/layout normalization.
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

        /// <summary>
        /// Resolves a project file or directory path to an existing project directory.
        /// </summary>
        /// <param name="path">The project file or directory path.</param>
        /// <param name="parameterName">The parameter label used in validation messages.</param>
        /// <returns>The normalized project directory.</returns>
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

        /// <summary>
        /// Builds the database-type-specific migration directory path.
        /// </summary>
        /// <param name="projectDirectory">The project directory.</param>
        /// <param name="databaseType">The database provider directory name.</param>
        /// <returns>The migration directory path.</returns>
        internal static string GetMigrationDirectory(string projectDirectory, string databaseType)
        {
            return Path.Combine(projectDirectory, "Domain", "Migrations", databaseType);
        }

        /// <summary>
        /// Converts a migration directory into a project-relative command argument.
        /// </summary>
        /// <param name="projectDirectory">The target project directory.</param>
        /// <param name="migrationDirectory">The migration output directory.</param>
        /// <returns>A relative migration directory argument.</returns>
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

        /// <summary>
        /// Derives the migration namespace from the project RootNamespace/AssemblyName and database type.
        /// </summary>
        /// <param name="projectDirectory">The project directory containing exactly one project file.</param>
        /// <param name="databaseType">The database provider directory name.</param>
        /// <returns>The expected migration namespace.</returns>
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

        /// <summary>
        /// Captures existing top-level migration files before an EF migration command runs.
        /// </summary>
        /// <param name="migrationDirectory">The migration directory.</param>
        /// <returns>Absolute paths of existing C# files.</returns>
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

        /// <summary>
        /// Verifies that exactly one new migration and matching Designer file were generated.
        /// </summary>
        /// <param name="migrationDirectory">The migration output directory.</param>
        /// <param name="filesBefore">Files captured before generation.</param>
        /// <returns>The generated migration and Designer file paths.</returns>
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

        /// <summary>
        /// Moves the matching model snapshot into the provider migration directory,
        /// removes duplicate snapshots, and rewrites its namespace when necessary.
        /// </summary>
        /// <param name="projectDirectory">The project directory to search.</param>
        /// <param name="migrationDirectory">The expected provider migration directory.</param>
        /// <param name="dbContextName">The DbContext name used to identify the snapshot.</param>
        /// <param name="expectedNamespace">The namespace required by the target project.</param>
        /// <returns>Snapshot movement, namespace, and duplicate-removal details.</returns>
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

        /// <summary>
        /// Compares two paths using platform-appropriate case semantics.
        /// </summary>
        /// <param name="first">The first path.</param>
        /// <param name="second">The second path.</param>
        /// <returns><see langword="true"/> when normalized paths are equal.</returns>
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
