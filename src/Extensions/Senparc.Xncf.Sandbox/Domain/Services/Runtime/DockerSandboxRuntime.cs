/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DockerSandboxRuntime.cs
    文件功能描述：基于 Docker CLI 的沙箱运行时

    创建标识：Senparc - 20260808

----------------------------------------------------------------*/

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// 通过 docker CLI 管理容器。标签：ncf.sandbox=1 / ncf.sandbox.session={id}
/// </summary>
public sealed class DockerSandboxRuntime : ISandboxRuntime
{
    public const string SandboxLabel = "ncf.sandbox=1";
    private readonly ILogger<DockerSandboxRuntime> _logger;
    private readonly ISandboxImageResolver _imageResolver;

    public DockerSandboxRuntime(ILogger<DockerSandboxRuntime> logger, ISandboxImageResolver imageResolver)
    {
        _logger = logger;
        _imageResolver = imageResolver ?? new SandboxImageResolver(new SandboxImageOptions());
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
        if (!request.Template.Interactive)
        {
            throw new InvalidOperationException($"模板 {request.Template.Key} 不是交互式模板。");
        }

        var hostPort = GetFreeTcpPort();
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        Directory.CreateDirectory(request.WorkspaceDirectory);
        var image = _imageResolver.Resolve(request.Template.Key, request.Template.Image);
        _logger.LogInformation("Sandbox interactive image resolved: template={Template} image={Image}", request.Template.Key, image);

        // base_url 与站点反向代理路径一致；token 仅存库并在服务端注入，不出现在对外 AccessUrl。
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

        var run = await RunDockerAsync(args, null, TimeSpan.FromMinutes(3), cancellationToken).ConfigureAwait(false);
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
            AccessUrl = SandboxJupyterPaths.GetLabEntryUrl(request.SessionId),
            AccessToken = token,
            Message = "JupyterLab 已启动；请通过站点反向代理访问（需管理员登录）。"
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
        if (string.Equals(request.Template.Key, SandboxTemplateKeys.PythonExec, StringComparison.OrdinalIgnoreCase))
        {
            fileName = "main.py";
            await File.WriteAllTextAsync(Path.Combine(workDir, fileName), request.Code, cancellationToken).ConfigureAwait(false);
            command = new[] { "python", $"/work/{fileName}" };
        }
        else if (string.Equals(request.Template.Key, SandboxTemplateKeys.CsharpExec, StringComparison.OrdinalIgnoreCase))
        {
            // .NET 10 file-based apps：单文件直接运行，无需 .csproj / dotnet new。
            fileName = "main.cs";
            await File.WriteAllTextAsync(Path.Combine(workDir, fileName), request.Code, cancellationToken).ConfigureAwait(false);
            command = new[]
            {
                "dotnet", "run", "--file", $"/work/{fileName}", "-v", "q"
            };
        }
        else
        {
            throw new InvalidOperationException($"模板 {request.Template.Key} 不支持 Exec。");
        }

        var image = _imageResolver.Resolve(request.Template.Key, request.Template.Image);
        _logger.LogInformation("Sandbox exec image resolved: template={Template} image={Image}", request.Template.Key, image);

        var args = new List<string>
        {
            "run", "--rm",
            "--label", "ncf.sandbox=1",
            "--label", $"ncf.sandbox.session={request.SessionId}",
            "--cpus", request.CpuLimit.ToString("0.###"),
            "--memory", $"{request.MemoryMb}m",
            "--pids-limit", "128",
            "--network", "none",
            "-v", $"{workDir}:/work:ro",
            image
        };
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
        var result = await RunDockerAsync(
                new[] { "ps", "-aq", "--filter", "label=ncf.sandbox=1" },
                null,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            return Array.Empty<string>();
        }

        return result.StdOut
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(z => z.Trim())
            .Where(z => z.Length > 0)
            .ToArray();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

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
