/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DockerSandboxRuntime.cs
    文件功能描述：基于 Docker CLI 的沙箱运行时

    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱工作负载

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

----------------------------------------------------------------*/

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// 通过 docker CLI 管理容器。标签：ncf.sandbox=1 / ncf.sandbox.session={id}
/// </summary>
public sealed class DockerSandboxRuntime : ISandboxRuntime
{
    public const string SandboxLabel = "ncf.sandbox=1";
    private readonly ILogger<DockerSandboxRuntime> _logger;
    private readonly ISandboxImageResolver _imageResolver;
    private readonly SandboxDockerOptions _dockerOptions;

    public DockerSandboxRuntime(
        ILogger<DockerSandboxRuntime> logger,
        ISandboxImageResolver imageResolver,
        IOptions<SandboxDockerOptions>? dockerOptions = null)
    {
        _logger = logger;
        _imageResolver = imageResolver ?? new SandboxImageResolver(new SandboxImageOptions());
        _dockerOptions = dockerOptions?.Value ?? new SandboxDockerOptions();
    }

    public SandboxRuntimeKind Kind => SandboxRuntimeKind.Docker;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await RunDockerAsync(new[] { "version", "--format", "{{.Server.Version}}" }, null, TimeSpan.FromSeconds(8), cancellationToken)
                .ConfigureAwait(false);
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StdOut);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Docker runtime probe failed.");
            return false;
        }
    }

    public async Task<SandboxCreateRuntimeResult> CreateInteractiveAsync(
        SandboxCreateRuntimeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.Template.Key, SandboxTemplateKeys.NcfPreview, StringComparison.OrdinalIgnoreCase))
        {
            return await CreateNcfPreviewAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (!request.Template.Interactive)
        {
            throw new InvalidOperationException($"模板 {request.Template.Key} 不是交互式模板。");
        }

        var hostPort = GetFreeTcpPort();
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        Directory.CreateDirectory(request.WorkspaceDirectory);
        var image = _imageResolver.Resolve(request.Template.Key, request.Template.Image);
        _logger.LogInformation("Sandbox interactive image resolved: template={Template} image={Image}", request.Template.Key, image);

        // base_url 与 Jupyter 路径一致；列表链接使用本机映射端口和 token 直达容器。
        var baseUrl = SandboxJupyterPaths.GetBaseUrl(request.SessionId);
        var args = new List<string>
        {
            "run", "-d",
            "--name", $"ncf-sandbox-{request.SessionId}",
            "--label", "ncf.sandbox=1",
            "--label", $"ncf.sandbox.session={request.SessionId}",
            "--cpus", request.CpuLimit.ToString("0.###"),
            "--memory", $"{request.MemoryMb}m",
            "--pids-limit", "256",
            "-p", $"127.0.0.1:{hostPort}:{request.Template.ContainerPort}",
            "-v", $"{request.WorkspaceDirectory}:/home/jovyan/work",
            "-e", $"JUPYTER_TOKEN={token}",
            image,
            "start-notebook.py",
            $"--ServerApp.token={token}",
            $"--ServerApp.base_url={baseUrl}",
            "--ServerApp.allow_origin=*",
            "--ServerApp.allow_remote_access=True",
            "--ServerApp.disable_check_xsrf=True",
            "--ServerApp.root_dir=/home/jovyan/work"
        };

        var createTimeout = _dockerOptions.GetInteractiveCreateTimeout();
        _logger.LogInformation(
            "Sandbox interactive container creating: session={SessionId} timeoutSeconds={TimeoutSeconds}",
            request.SessionId,
            createTimeout.TotalSeconds);
        var run = await RunDockerAsync(args, null, createTimeout, cancellationToken).ConfigureAwait(false);
        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker 启动失败: {run.StdErr}\n{run.StdOut}");
        }

        var containerId = run.StdOut.Trim();
        _logger.LogInformation(
            "Sandbox Jupyter started: session={SessionId} container={ContainerId} hostPort={HostPort} baseUrl={BaseUrl}",
            request.SessionId,
            containerId,
            hostPort,
            baseUrl);

        return new SandboxCreateRuntimeResult
        {
            RuntimeHandle = containerId,
            HostPort = hostPort,
            AccessUrl = SandboxJupyterPaths.GetDirectLabEntryUrl(request.SessionId, hostPort, token),
            AccessToken = token,
            Message = "JupyterLab 已启动；请使用本机映射端口打开。"
        };
    }

    /// <summary>
    /// Starts one fixed NCF preview sequence. There is deliberately no user-provided command,
    /// Docker socket, host networking, capability or writable host checkout in this workload.
    /// The supplied workspace is already Sandbox-owned and may be mutated only inside the
    /// container for obj/bin/publish output.
    /// </summary>
    private async Task<SandboxCreateRuntimeResult> CreateNcfPreviewAsync(
        SandboxCreateRuntimeRequest request,
        CancellationToken cancellationToken)
    {
        var preview = request.NcfPreview
                      ?? throw new InvalidOperationException("NCF 预览缺少服务器生成的运行配置。");
        ValidateNcfPreviewValue(preview.SolutionRelativePath, nameof(preview.SolutionRelativePath));
        ValidateNcfPreviewValue(preview.ModuleProjectName, nameof(preview.ModuleProjectName));
        var previewBasePath = string.IsNullOrWhiteSpace(preview.BasePath)
            ? SandboxNcfPreviewPaths.GetBasePath(request.SessionId)
            : preview.BasePath;
        var hostPort = GetFreeTcpPort();
        Directory.CreateDirectory(request.WorkspaceDirectory);
        var image = _imageResolver.Resolve(request.Template.Key, request.Template.Image);
        if (!image.Contains("@sha256:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "NCF Sandbox 预览镜像必须由 SenparcXncfSandbox:Images:Overrides:ncf-preview 配置为不可变 digest（例如 image@sha256:...）。");
        }

        var network = preview.AllowDependencyRestoreNetwork
            ? preview.RestoreNetworkName
            : "none";
        if (string.IsNullOrWhiteSpace(network)
            || (preview.AllowDependencyRestoreNetwork && !IsSafeDockerNetworkName(network)))
        {
            throw new InvalidOperationException("NCF Sandbox 预览的依赖还原网络未配置为受控 Docker 网络。");
        }

        _logger.LogInformation(
            "Sandbox NCF preview image resolved: image={Image} network={Network} session={SessionId}",
            image,
            network,
            request.SessionId);

        var args = new List<string>
        {
            "run", "-d",
            "--name", $"ncf-sandbox-{request.SessionId}",
            "--label", "ncf.sandbox=1",
            "--label", $"ncf.sandbox.session={request.SessionId}",
            "--cpus", request.CpuLimit.ToString("0.###"),
            "--memory", $"{request.MemoryMb}m",
            "--pids-limit", "256",
            "--cap-drop", "ALL",
            "--security-opt", "no-new-privileges",
            "--read-only",
            "--tmpfs", "/tmp:rw,noexec,nosuid,size=256m",
            "--network", network,
            "-p", $"127.0.0.1:{hostPort}:{request.Template.ContainerPort}",
            "-v", $"{request.WorkspaceDirectory}:/workspace",
            "-w", "/workspace",
            "-e", "HOME=/tmp/home",
            "-e", "DOTNET_CLI_HOME=/tmp/dotnet-home",
            "-e", "DOTNET_CLI_TELEMETRY_OPTOUT=1",
            "-e", $"NCF_SOLUTION_RELATIVE_PATH={preview.SolutionRelativePath.Replace('\\', '/')}",
            "-e", $"NCF_XNCF_PREVIEW_PATH_BASE={previewBasePath}",
            image,
            "sh", "-c", NcfPreviewLaunchScript
        };

        var run = await RunDockerAsync(
                args,
                null,
                TimeSpan.FromSeconds(Math.Clamp(preview.StartupTimeoutSeconds + 30, 60, 600)),
                cancellationToken)
            .ConfigureAwait(false);
        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker NCF 预览启动失败: {run.StdErr}\n{run.StdOut}");
        }

        var containerId = run.StdOut.Trim();
        try
        {
            await WaitForOpenTcpPortAsync(
                    hostPort,
                    TimeSpan.FromSeconds(Math.Clamp(preview.StartupTimeoutSeconds, 30, 600)),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            var logs = await RunDockerAsync(new[] { "logs", "--tail", "160", containerId }, null, TimeSpan.FromSeconds(20), CancellationToken.None)
                .ConfigureAwait(false);
            await DestroyAsync(containerId, CancellationToken.None).ConfigureAwait(false);
            throw new InvalidOperationException($"NCF Sandbox 预览未在规定时间内监听端口。\n{logs.StdErr}\n{logs.StdOut}");
        }

        return new SandboxCreateRuntimeResult
        {
            RuntimeHandle = containerId,
            HostPort = hostPort,
            AccessUrl = SandboxNcfPreviewPaths.GetEntryUrl(request.SessionId),
            Message = "NCF Sandbox 预览已启动；仅通过管理员鉴权的反向代理访问。"
        };
    }

    public async Task<SandboxExecResult> ExecAsync(
        SandboxExecRequest request,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetTempPath());
        var workDir = Path.Combine(Path.GetTempPath(), "Senparc.Ncf", "SandboxExec", request.SessionId);
        Directory.CreateDirectory(workDir);

        string fileName;
        string[] command;
        var mountReadOnly = true;
        var extraDockerArgs = new List<string>();

        if (string.Equals(request.Template.Key, SandboxTemplateKeys.PythonExec, StringComparison.OrdinalIgnoreCase))
        {
            fileName = "main.py";
            await File.WriteAllTextAsync(Path.Combine(workDir, fileName), request.Code, cancellationToken).ConfigureAwait(false);
            command = new[] { "python", $"/work/{fileName}" };
        }
        else if (string.Equals(request.Template.Key, SandboxTemplateKeys.CsharpExec, StringComparison.OrdinalIgnoreCase))
        {
            // .NET 10 file-based apps：单文件运行。Exec 容器无外网，需关闭默认 AOT，并清空 NuGet 在线源。
            fileName = "main.cs";
            await File.WriteAllTextAsync(Path.Combine(workDir, fileName), request.Code, cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(workDir, "nuget.config"), OfflineNuGetConfig, cancellationToken)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(workDir, "Directory.Build.props"), OfflineDirectoryBuildProps, cancellationToken)
                .ConfigureAwait(false);

            mountReadOnly = false; // 编译需要写 obj/bin
            extraDockerArgs.AddRange(new[]
            {
                "-e", "HOME=/tmp",
                "-e", "DOTNET_CLI_HOME=/tmp/dotnet-home",
                "-e", "DOTNET_CLI_TELEMETRY_OPTOUT=1",
                "-w", "/work"
            });
            command = new[]
            {
                "dotnet", "run", "--file", $"/work/{fileName}", "-v", "q",
                "-p:PublishAot=false",
                "-p:NuGetAudit=false"
            };
        }
        else
        {
            throw new InvalidOperationException($"模板 {request.Template.Key} 不支持 Exec。");
        }

        var image = _imageResolver.Resolve(request.Template.Key, request.Template.Image);
        _logger.LogInformation(
            "Sandbox exec image resolved: template={Template} image={Image} network=none mountRo={MountRo}",
            request.Template.Key,
            image,
            mountReadOnly);

        var args = new List<string>
        {
            "run", "--rm",
            "--label", "ncf.sandbox=1",
            "--label", $"ncf.sandbox.session={request.SessionId}",
            "--cpus", request.CpuLimit.ToString("0.###"),
            "--memory", $"{request.MemoryMb}m",
            "--pids-limit", "128",
            "--network", "none",
            "-v", mountReadOnly ? $"{workDir}:/work:ro" : $"{workDir}:/work"
        };
        args.AddRange(extraDockerArgs);
        args.Add(image);
        args.AddRange(command);

        var run = await RunDockerAsync(args, null, request.Timeout, cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.Delete(workDir, true);
        }
        catch
        {
            // ignore cleanup failures
        }

        return new SandboxExecResult
        {
            ExitCode = run.ExitCode,
            StdOut = run.StdOut,
            StdErr = run.StdErr
        };
    }

    public async Task DestroyAsync(string runtimeHandle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runtimeHandle))
        {
            return;
        }

        await RunDockerAsync(new[] { "rm", "-f", runtimeHandle }, null, TimeSpan.FromSeconds(30), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> ListOrphanHandlesAsync(CancellationToken cancellationToken = default)
    {
        return await ListHandlesAsync(all: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> ListRunningHandlesAsync(CancellationToken cancellationToken = default)
    {
        return await ListHandlesAsync(all: false, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<string>> ListHandlesAsync(bool all, CancellationToken cancellationToken)
    {
        var result = await RunDockerAsync(
                all
                    ? new[] { "ps", "-aq", "--no-trunc", "--filter", "label=ncf.sandbox=1" }
                    : new[] { "ps", "-q", "--no-trunc", "--filter", "label=ncf.sandbox=1" },
                null,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker 查询沙箱容器失败：{result.StdErr}");
        }

        return result.StdOut
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(z => z.Trim())
            .Where(z => z.Length > 0)
            .ToArray();
    }

    /// <summary>
    /// 清空在线源，避免 --network none 时 NU1301；BCL 由 SDK 镜像自带，无需 nuget.org。
    /// </summary>
    private const string OfflineNuGetConfig =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
          </packageSources>
        </configuration>
        """;

    /// <summary>
    /// file-based apps 默认 PublishAot=true，离线无法还原 ILCompiler；沙箱 Exec 关闭 AOT/审计。
    /// </summary>
    private const string OfflineDirectoryBuildProps =
        """
        <Project>
          <PropertyGroup>
            <PublishAot>false</PublishAot>
            <NuGetAudit>false</NuGetAudit>
          </PropertyGroup>
        </Project>
        """;

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForOpenTcpPortAsync(int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        Exception? lastError = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                lastError = ex;
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new TimeoutException($"NCF preview port {port} did not become available: {lastError?.Message}");
    }

    private static bool IsSafeDockerNetworkName(string value) =>
        value.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.');

    private static void ValidateNcfPreviewValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || Path.IsPathRooted(value)
            || value.Contains("..", StringComparison.Ordinal)
            || value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
        {
            throw new ArgumentException("NCF 预览路径参数无效。", parameterName);
        }
    }

    private const string NcfPreviewLaunchScript = """
        set -eu
        solution="/workspace/${NCF_SOLUTION_RELATIVE_PATH}"
        solution_dir="$(dirname "$solution")"
        web_project="$solution_dir/Senparc.Web/Senparc.Web.csproj"
        test -f "$solution"
        test -f "$web_project"
        dotnet restore "$web_project" --ignore-failed-sources --disable-parallel
        dotnet publish "$web_project" --no-restore --no-self-contained --configuration Debug --output /workspace/.ncf-preview-publish --disable-build-servers -m:1 /p:UseAppHost=false
        exec dotnet /workspace/.ncf-preview-publish/Senparc.Web.dll --urls=http://0.0.0.0:8080 --environment=XncfPreview
        """;

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunDockerAsync(
        IReadOnlyList<string> args,
        string? workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动 docker 进程。请确认已安装 Docker 且 docker 在 PATH 中。");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException($"docker 命令超时（{timeout.TotalSeconds:0}s）：docker {string.Join(' ', args)}");
        }

        return (process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
