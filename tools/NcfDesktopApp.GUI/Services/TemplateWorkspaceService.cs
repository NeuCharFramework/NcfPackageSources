/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：TemplateWorkspaceService.cs
    文件功能描述：从官方 NuGet 模板创建隔离的 NCF 源码工作区

    创建标识：Senparc - 20260730
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NcfDesktopApp.GUI.Models;

namespace NcfDesktopApp.GUI.Services;

internal sealed record TemplateWorkspaceResult(
    string WorkspacePath,
    NcfLaunchTarget LaunchTarget,
    string TemplateInstallOutput,
    string TemplateCreateOutput);

internal sealed class TemplateWorkspaceService
{
    internal const string TemplatePackageId = "Senparc.NCF.Template";
    internal const string TemplateShortName = "NCF";

    internal async Task<TemplateWorkspaceResult> CreateAsync(
        string parentDirectory,
        string workspaceName,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        var targetPath = ValidateAndGetTargetPath(parentDirectory, workspaceName);
        var sdk = DotnetSdkResolver.Resolve("net10.0", parentDirectory);
        if (!sdk.IsValid)
        {
            throw new InvalidOperationException(sdk.ErrorMessage);
        }

        onOutput?.Invoke($"正在从 NuGet 安装或更新模板 {TemplatePackageId}…");
        var installOutput = await RunDotnetAsync(
            sdk.DotnetPath!,
            parentDirectory,
            new[] { "new", "install", TemplatePackageId, "--force", "--verbosity", "minimal" },
            onOutput,
            cancellationToken).ConfigureAwait(false);

        onOutput?.Invoke($"正在创建工作区 {targetPath}（不执行 restore）…");
        var templateHelp = await RunDotnetAsync(
            sdk.DotnetPath!,
            parentDirectory,
            new[] { "new", TemplateShortName, "--help" },
            onOutput: null,
            cancellationToken).ConfigureAwait(false);
        var createArguments = new List<string>
        {
            "new", TemplateShortName,
            "--name", workspaceName.Trim(),
            "--output", targetPath
        };
        // Restore post actions expose this host option. The current 0.35.0
        // template has no restore post action and therefore rejects the flag.
        if (templateHelp.Contains("--no-restore", StringComparison.Ordinal))
        {
            createArguments.Add("--no-restore");
        }
        var createOutput = await RunDotnetAsync(
            sdk.DotnetPath!,
            parentDirectory,
            createArguments.ToArray(),
            onOutput,
            cancellationToken).ConfigureAwait(false);

        var resolution = NcfLaunchTargetResolver.ResolveExternal(targetPath);
        if (!resolution.IsValid)
        {
            throw new InvalidOperationException(
                $"模板已生成，但没有找到可启动的 Senparc.Web.csproj：{resolution.ErrorMessage}");
        }

        return new TemplateWorkspaceResult(targetPath, resolution.Target!, installOutput, createOutput);
    }

    internal static string ValidateAndGetTargetPath(string parentDirectory, string workspaceName)
    {
        if (string.IsNullOrWhiteSpace(parentDirectory) || !Directory.Exists(parentDirectory))
        {
            throw new DirectoryNotFoundException("请选择一个已经存在且可写的工作区父目录。");
        }

        var name = workspaceName?.Trim() ?? string.Empty;
        if (name.Length == 0 || name is "." or ".." ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            name.Contains(Path.DirectorySeparatorChar) ||
            name.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("工作区名称不能为空，也不能包含路径分隔符或无效文件名字符。", nameof(workspaceName));
        }

        var parentPath = Path.GetFullPath(parentDirectory);
        var targetPath = Path.GetFullPath(Path.Combine(parentPath, name));
        var parentPrefix = parentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                           Path.DirectorySeparatorChar;
        if (!targetPath.StartsWith(parentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("工作区路径必须位于所选父目录内。");
        }

        if (Directory.Exists(targetPath) && Directory.EnumerateFileSystemEntries(targetPath).Any())
        {
            throw new IOException("目标工作区已经存在且不为空；请选择新名称，现有内容不会被覆盖。");
        }

        if (Directory.Exists(targetPath) && !string.IsNullOrWhiteSpace(new DirectoryInfo(targetPath).LinkTarget))
        {
            throw new IOException("目标工作区不能是符号链接；请选择新的普通目录名称。");
        }

        if (File.Exists(targetPath))
        {
            throw new IOException("目标路径已被文件占用。");
        }

        return targetPath;
    }

    private static async Task<string> RunDotnetAsync(
        string dotnetPath,
        string workingDirectory,
        string[] arguments,
        Action<string>? onOutput,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = dotnetPath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("无法启动 dotnet CLI。");
        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // 取消时进程可能已经退出。
            }
        });

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);
        var combinedOutput = string.Join(
            Environment.NewLine,
            new[] { standardOutput.Trim(), standardError.Trim() }.Where(value => value.Length > 0));
        if (combinedOutput.Length > 0)
        {
            onOutput?.Invoke(combinedOutput);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet {string.Join(' ', arguments.Take(3))} 执行失败（ExitCode: {process.ExitCode}）。{Environment.NewLine}{combinedOutput}");
        }

        return combinedOutput;
    }
}
