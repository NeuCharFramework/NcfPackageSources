/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcfLaunchTargetResolver.cs
    文件功能描述：识别托管发布目录、外部发布目录和 NCF 源码项目


    创建标识：Senparc - 20260725

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Linq;
using NcfDesktopApp.GUI.Models;

namespace NcfDesktopApp.GUI.Services;

public static class NcfLaunchTargetResolver
{
    private const int ManagedPublishedSearchDepth = 2;
    private const int ExternalPublishedSearchDepth = 2;
    private const int SourceProjectSearchDepth = 6;
    private const int MaxScannedDirectories = 3000;

    private static readonly HashSet<string> SkippedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", ".vscode", "bin", "obj", "node_modules", "packages"
    };

    public static NcfLaunchTargetResolution ResolveManagedRuntime(string runtimePath)
    {
        var normalized = NormalizePath(runtimePath);
        if (normalized == null || !Directory.Exists(normalized))
        {
            return NcfLaunchTargetResolution.Failure("内置 Runtime 目录不存在，请先安装 NCF。");
        }

        var candidates = FindPublishedDirectories(normalized, ManagedPublishedSearchDepth);
        if (candidates.Count == 0)
        {
            return NcfLaunchTargetResolution.Failure("内置 Runtime 中未找到 Senparc.Web 启动文件。");
        }

        return NcfLaunchTargetResolution.Success(CreatePublishedTarget(
            NcfLaunchTargetKind.ManagedPublished,
            normalized,
            candidates[0]));
    }

    public static NcfLaunchTargetResolution ResolveExternal(string? selectedPath)
    {
        var normalized = NormalizePath(selectedPath);
        if (normalized == null)
        {
            return NcfLaunchTargetResolution.Failure("请选择 NCF 发布目录或包含 Senparc.Web.csproj 的源码目录。");
        }

        if (File.Exists(normalized))
        {
            if (string.Equals(Path.GetFileName(normalized), "Senparc.Web.csproj", StringComparison.OrdinalIgnoreCase))
            {
                return NcfLaunchTargetResolution.Success(CreateSourceTarget(normalized, normalized));
            }

            if (IsPublishedEntryFile(normalized))
            {
                var directory = Path.GetDirectoryName(normalized)!;
                return NcfLaunchTargetResolution.Success(CreatePublishedTarget(
                    NcfLaunchTargetKind.ExternalPublished,
                    normalized,
                    directory));
            }

            return NcfLaunchTargetResolution.Failure("所选文件不是 Senparc.Web.csproj、Senparc.Web.dll 或平台可执行文件。");
        }

        if (!Directory.Exists(normalized))
        {
            return NcfLaunchTargetResolution.Failure("所选路径不存在或当前用户无权访问。");
        }

        if (ContainsPublishedEntry(normalized))
        {
            return NcfLaunchTargetResolution.Success(CreatePublishedTarget(
                NcfLaunchTargetKind.ExternalPublished,
                normalized,
                normalized));
        }

        var sourceProjects = FindFiles(normalized, "Senparc.Web.csproj", SourceProjectSearchDepth);
        if (sourceProjects.Count == 1)
        {
            return NcfLaunchTargetResolution.Success(CreateSourceTarget(normalized, sourceProjects[0]));
        }

        if (sourceProjects.Count > 1)
        {
            return NcfLaunchTargetResolution.Failure(
                $"目录中发现 {sourceProjects.Count} 个 Senparc.Web.csproj，请选择具体项目所在目录。");
        }

        var publishedDirectories = FindPublishedDirectories(normalized, ExternalPublishedSearchDepth);
        if (publishedDirectories.Count == 1)
        {
            return NcfLaunchTargetResolution.Success(CreatePublishedTarget(
                NcfLaunchTargetKind.ExternalPublished,
                normalized,
                publishedDirectories[0]));
        }

        if (publishedDirectories.Count > 1)
        {
            return NcfLaunchTargetResolution.Failure(
                $"目录中发现 {publishedDirectories.Count} 个可启动发布目录，请选择具体的 Senparc.Web 输出目录。");
        }

        return NcfLaunchTargetResolution.Failure(
            "未找到 NCF 入口。发布目录需包含 Senparc.Web.dll/可执行文件，源码目录需包含 Senparc.Web.csproj。");
    }

    public static NcfLaunchTargetResolution ResolveRemote(string? siteUrl)
    {
        if (!SiteEndpointPolicy.TryNormalizeSiteUrl(siteUrl, out var siteUri, out var errorMessage))
        {
            return NcfLaunchTargetResolution.Failure(errorMessage);
        }

        var normalizedUrl = siteUri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return NcfLaunchTargetResolution.Success(new NcfLaunchTarget(
            NcfLaunchTargetKind.RemoteSite,
            normalizedUrl,
            string.Empty,
            normalizedUrl,
            siteUri.Host,
            "由远程站点报告",
            siteUri.Scheme == Uri.UriSchemeHttps ? "HTTPS" : "本机隧道/回环 HTTP"));
    }

    private static NcfLaunchTarget CreatePublishedTarget(
        NcfLaunchTargetKind kind,
        string selectedPath,
        string appDirectory)
    {
        var entryPath = ResolvePublishedEntryPath(appDirectory);
        var version = ReadPublishedVersion(selectedPath, appDirectory, entryPath);
        var framework = ReadRuntimeTargetFramework(appDirectory);
        var displayName = Path.GetFileName(appDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Senparc.Web";
        }

        return new NcfLaunchTarget(
            kind,
            selectedPath,
            appDirectory,
            entryPath,
            displayName,
            version,
            framework);
    }

    private static NcfLaunchTarget CreateSourceTarget(string selectedPath, string projectPath)
    {
        var (version, framework) = ReadProjectMetadata(projectPath);
        return new NcfLaunchTarget(
            NcfLaunchTargetKind.SourceProject,
            selectedPath,
            Path.GetDirectoryName(projectPath)!,
            projectPath,
            Path.GetFileName(Path.GetDirectoryName(projectPath)!) ?? "Senparc.Web",
            version,
            framework);
    }

    private static List<string> FindPublishedDirectories(string rootPath, int maxDepth)
    {
        var results = new List<string>();
        var queue = new Queue<(string Path, int Depth)>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        queue.Enqueue((rootPath, 0));

        while (queue.Count > 0 && visited.Count < MaxScannedDirectories)
        {
            var (current, depth) = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }
            if (ContainsPublishedEntry(current))
            {
                results.Add(current);
                continue;
            }

            if (depth >= maxDepth)
            {
                continue;
            }

            foreach (var directory in EnumerateDirectoriesSafely(current))
            {
                if (!ShouldSkipDirectory(directory))
                {
                    queue.Enqueue((directory, depth + 1));
                }
            }
        }

        return results
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path.Count(c => c == Path.DirectorySeparatorChar))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> FindFiles(string rootPath, string fileName, int maxDepth)
    {
        var results = new List<string>();
        var queue = new Queue<(string Path, int Depth)>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        queue.Enqueue((rootPath, 0));

        while (queue.Count > 0 && visited.Count < MaxScannedDirectories)
        {
            var (current, depth) = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }
            try
            {
                var candidate = Path.Combine(current, fileName);
                if (File.Exists(candidate))
                {
                    results.Add(candidate);
                }
            }
            catch
            {
                // 单个目录不可访问时继续扫描其它候选目录。
            }

            if (depth >= maxDepth)
            {
                continue;
            }

            foreach (var directory in EnumerateDirectoriesSafely(current))
            {
                if (!ShouldSkipDirectory(directory))
                {
                    queue.Enqueue((directory, depth + 1));
                }
            }
        }

        return results
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> EnumerateDirectoriesSafely(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static bool ShouldSkipDirectory(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return SkippedDirectoryNames.Contains(name);
    }

    private static bool ContainsPublishedEntry(string directory)
    {
        return File.Exists(Path.Combine(directory, "Senparc.Web.dll"))
               || File.Exists(Path.Combine(directory, "Senparc.Web.exe"))
               || File.Exists(Path.Combine(directory, "Senparc.Web"));
    }

    private static bool IsPublishedEntryFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return string.Equals(fileName, "Senparc.Web.dll", StringComparison.OrdinalIgnoreCase)
               || string.Equals(fileName, "Senparc.Web.exe", StringComparison.OrdinalIgnoreCase)
               || string.Equals(fileName, "Senparc.Web", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolvePublishedEntryPath(string appDirectory)
    {
        var windowsExecutable = Path.Combine(appDirectory, "Senparc.Web.exe");
        var unixExecutable = Path.Combine(appDirectory, "Senparc.Web");
        var dll = Path.Combine(appDirectory, "Senparc.Web.dll");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(windowsExecutable))
        {
            return windowsExecutable;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(unixExecutable))
        {
            return unixExecutable;
        }

        if (File.Exists(dll))
        {
            return dll;
        }

        return File.Exists(windowsExecutable) ? windowsExecutable : unixExecutable;
    }

    private static string ReadPublishedVersion(string selectedPath, string appDirectory, string entryPath)
    {
        try
        {
            var selectedDirectory = Directory.Exists(selectedPath)
                ? selectedPath
                : Path.GetDirectoryName(selectedPath);
            var versionFiles = new[]
            {
                Path.Combine(appDirectory, "version.txt"),
                string.IsNullOrWhiteSpace(selectedDirectory)
                    ? string.Empty
                    : Path.Combine(selectedDirectory, "version.txt")
            };
            foreach (var versionFile in versionFiles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(versionFile))
                {
                    var version = File.ReadAllText(versionFile).Trim();
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        return version;
                    }
                }
            }

            var dll = Path.Combine(appDirectory, "Senparc.Web.dll");
            var versionPath = File.Exists(dll) ? dll : entryPath;
            var fileVersion = FileVersionInfo.GetVersionInfo(versionPath).ProductVersion;
            return string.IsNullOrWhiteSpace(fileVersion) ? "未标记" : fileVersion;
        }
        catch
        {
            return "未标记";
        }
    }

    private static string ReadRuntimeTargetFramework(string appDirectory)
    {
        var runtimeConfigPath = Path.Combine(appDirectory, "Senparc.Web.runtimeconfig.json");
        if (!File.Exists(runtimeConfigPath))
        {
            return "自包含或未标记";
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(runtimeConfigPath));
            if (document.RootElement.TryGetProperty("runtimeOptions", out var runtimeOptions)
                && runtimeOptions.TryGetProperty("tfm", out var tfm))
            {
                return tfm.GetString() ?? "未标记";
            }
        }
        catch
        {
            // 损坏的 runtimeconfig 由实际启动日志继续诊断。
        }

        return "未标记";
    }

    private static (string Version, string Framework) ReadProjectMetadata(string projectPath)
    {
        try
        {
            var document = XDocument.Load(projectPath);
            var properties = document.Descendants().Where(element => element.Parent?.Name.LocalName == "PropertyGroup").ToList();
            var version = properties.FirstOrDefault(element => element.Name.LocalName == "Version")?.Value?.Trim();
            var framework = properties.FirstOrDefault(element => element.Name.LocalName == "TargetFramework")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(framework))
            {
                framework = properties.FirstOrDefault(element => element.Name.LocalName == "TargetFrameworks")?.Value?.Trim();
            }

            return (
                string.IsNullOrWhiteSpace(version) ? "源码工作区" : version,
                string.IsNullOrWhiteSpace(framework) ? "由项目配置决定" : framework);
        }
        catch
        {
            return ("源码工作区", "项目文件解析失败");
        }
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path.Trim().Trim('"'));
        }
        catch
        {
            return null;
        }
    }
}
