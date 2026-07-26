/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DotnetSdkResolver.cs
    文件功能描述：在桌面 GUI 环境中定位并验证可用的 .NET SDK


    创建标识：Senparc - 20260726

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NcfDesktopApp.GUI.Services;

internal sealed record DotnetSdkResolution(
    string? DotnetPath,
    int RequiredMajorVersion,
    string? SelectedSdkVersion,
    IReadOnlyList<string> DetectedSdkVersions,
    string ErrorMessage)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(DotnetPath);
}

internal readonly record struct DotnetCommandResult(
    bool Success,
    string StandardOutput,
    string StandardError,
    int ExitCode)
{
    public static DotnetCommandResult Failed(string error) => new(false, string.Empty, error, -1);
}

internal static class DotnetSdkResolver
{
    private const int DefaultRequiredMajorVersion = 10;

    internal static DotnetSdkResolution Resolve(
        string targetFramework,
        string workingDirectory,
        IEnumerable<string>? candidatePaths = null,
        Func<string, string, string, DotnetCommandResult>? commandRunner = null)
    {
        var requiredMajorVersion = GetTargetFrameworkMajorVersion(targetFramework);
        var candidates = (candidatePaths ?? GetCandidatePaths()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var runner = commandRunner ?? RunDotnetCommand;
        var useDefaultRunner = commandRunner == null;
        var detectedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reachableCandidateFound = false;

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (useDefaultRunner && Path.IsPathRooted(candidate) && !File.Exists(candidate))
            {
                continue;
            }

            var listResult = runner(candidate, "--list-sdks", workingDirectory);
            if (!listResult.Success)
            {
                continue;
            }

            reachableCandidateFound = true;
            var versions = ParseSdkVersions(listResult.StandardOutput);
            foreach (var version in versions)
            {
                detectedVersions.Add(version);
            }

            if (!versions.Any(version => TryGetMajorVersion(version, out var major) && major >= requiredMajorVersion))
            {
                continue;
            }

            // --version 会应用工作区中的 global.json，可避免“已安装但当前项目不可选用”的误判。
            var selectedResult = runner(candidate, "--version", workingDirectory);
            var selectedVersion = selectedResult.Success
                ? selectedResult.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()?.Trim()
                : null;

            if (!TryGetMajorVersion(selectedVersion, out var selectedMajor)
                || selectedMajor < requiredMajorVersion)
            {
                continue;
            }

            return new DotnetSdkResolution(
                candidate,
                requiredMajorVersion,
                selectedVersion,
                detectedVersions.OrderBy(version => version, StringComparer.OrdinalIgnoreCase).ToArray(),
                string.Empty);
        }

        var versionsText = detectedVersions.Count == 0
            ? "未检测到任何 SDK"
            : $"已检测 SDK: {string.Join(", ", detectedVersions.OrderBy(version => version, StringComparer.OrdinalIgnoreCase))}";
        var reason = reachableCandidateFound
            ? $"未找到可用于 net{requiredMajorVersion}.0 的 SDK。{versionsText}。"
            : "未找到可执行的 dotnet CLI。macOS App 不会继承终端 PATH，请确认系统安装目录可访问。";

        return new DotnetSdkResolution(
            null,
            requiredMajorVersion,
            null,
            detectedVersions.OrderBy(version => version, StringComparer.OrdinalIgnoreCase).ToArray(),
            $"{reason} 源码模式不会自动安装 SDK 或执行 restore。");
    }

    internal static IReadOnlyList<string> GetCandidatePaths()
    {
        var candidates = new List<string>();
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        void AddPath(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                candidates.Add(path);
            }
        }

        void AddRoot(string? root)
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
                AddPath(Path.Combine(root, executableName));
            }
        }

        AddPath(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH"));
        AddRoot(Environment.GetEnvironmentVariable("DOTNET_ROOT"));
        AddRoot(Environment.GetEnvironmentVariable("DOTNET_ROOT_ARM64"));
        AddRoot(Environment.GetEnvironmentVariable("DOTNET_ROOT_X64"));

        try
        {
            AddPath(Path.GetFullPath(Path.Combine(
                RuntimeEnvironment.GetRuntimeDirectory(),
                "..", "..", "..", executableName)));
        }
        catch
        {
            // 自包含运行时或受限目录下无法推导宿主根目录时继续检查标准安装位置。
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AddRoot(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"));
            AddRoot(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            AddPath("/usr/local/share/dotnet/dotnet");
            AddPath("/opt/homebrew/bin/dotnet");
            AddPath("/usr/local/bin/dotnet");
        }
        else
        {
            AddPath("/usr/share/dotnet/dotnet");
            AddPath("/usr/local/share/dotnet/dotnet");
            AddPath("/snap/bin/dotnet");
        }

        // 最后才依赖 GUI 进程继承到的 PATH。
        AddPath(executableName);
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    internal static IReadOnlyList<string> ParseSdkVersions(string output)
    {
        return (output ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Where(version => !string.IsNullOrWhiteSpace(version))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static int GetTargetFrameworkMajorVersion(string targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            return DefaultRequiredMajorVersion;
        }

        var firstFramework = targetFramework
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(firstFramework))
        {
            return DefaultRequiredMajorVersion;
        }

        string versionText;
        const string runtimePrefix = ".NETCoreApp,Version=v";
        if (firstFramework.StartsWith(runtimePrefix, StringComparison.OrdinalIgnoreCase))
        {
            versionText = firstFramework[runtimePrefix.Length..];
        }
        else if (firstFramework.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
        {
            versionText = firstFramework[10..];
        }
        else if (firstFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            versionText = firstFramework[3..];
        }
        else
        {
            return DefaultRequiredMajorVersion;
        }

        return TryGetMajorVersion(versionText, out var majorVersion)
            ? majorVersion
            : DefaultRequiredMajorVersion;
    }

    internal static string? GetDotnetRoot(string dotnetPath)
    {
        if (string.IsNullOrWhiteSpace(dotnetPath) || !Path.IsPathRooted(dotnetPath))
        {
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(dotnetPath);
            var resolved = fileInfo.ResolveLinkTarget(returnFinalTarget: true);
            return Path.GetDirectoryName((resolved ?? fileInfo).FullName);
        }
        catch
        {
            return Path.GetDirectoryName(dotnetPath);
        }
    }

    private static bool TryGetMajorVersion(string? version, out int majorVersion)
    {
        majorVersion = 0;
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var separatorIndex = version.IndexOfAny(new[] { '.', '-' });
        var majorText = separatorIndex >= 0 ? version[..separatorIndex] : version;
        return int.TryParse(majorText, out majorVersion) && majorVersion > 0;
    }

    private static DotnetCommandResult RunDotnetCommand(string dotnetPath, string argument, string workingDirectory)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = dotnetPath,
                WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : AppContext.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return DotnetCommandResult.Failed("无法启动 dotnet CLI");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return DotnetCommandResult.Failed("dotnet CLI 探测超时");
            }

            return new DotnetCommandResult(process.ExitCode == 0, output, error, process.ExitCode);
        }
        catch (Exception ex)
        {
            return DotnetCommandResult.Failed(ex.Message);
        }
    }
}
