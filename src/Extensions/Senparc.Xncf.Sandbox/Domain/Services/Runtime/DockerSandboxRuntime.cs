/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DockerSandboxRuntime.cs
    文件功能描述：基于 Docker CLI 的沙箱运行时

    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱工作负载

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

    修改标识：Senparc - 20260918
    修改描述：v0.3.3 支持创建时附加端口映射与交互式标准输入

    修改标识：Senparc - 20261005
    修改描述：v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Shared.Abstractions.Isolation;
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
    private readonly IIsolatedRuntimeProvider _runtimeProvider;

    public DockerSandboxRuntime(
        ILogger<DockerSandboxRuntime> logger,
        ISandboxImageResolver imageResolver,
        IEnumerable<IIsolatedRuntimeProvider> isolatedRuntimeProviders,
        IOptions<SandboxDockerOptions>? dockerOptions = null)
    {
        _logger = logger;
        _imageResolver = imageResolver ?? new SandboxImageResolver(new SandboxImageOptions());
        _dockerOptions = dockerOptions?.Value ?? new SandboxDockerOptions();
        _runtimeProvider = isolatedRuntimeProviders.Single(provider =>
            string.Equals(provider.ProviderName, "Docker", StringComparison.OrdinalIgnoreCase));
    }

    public SandboxRuntimeKind Kind => SandboxRuntimeKind.Docker;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return await _runtimeProvider.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
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

        var resolvedExtraPorts = ResolveExtraPortMappings(
            request.ExtraPortMappings,
            request.Template.ContainerPort,
            hostPort);

        var baseUrl = SandboxJupyterPaths.GetBaseUrl(request.SessionId);
        var ports = new List<IsolatedRuntimePortBinding>
        {
            new("127.0.0.1", hostPort, request.Template.ContainerPort)
        };
        foreach (var extraPort in resolvedExtraPorts)
        {
            ports.Add(new IsolatedRuntimePortBinding(
                extraPort.ExposeExternally ? "0.0.0.0" : "127.0.0.1",
                extraPort.HostPort,
                extraPort.ContainerPort));
        }

        var createTimeout = _dockerOptions.GetInteractiveCreateTimeout();
        _logger.LogInformation(
            "Sandbox interactive container creating: session={SessionId} timeoutSeconds={TimeoutSeconds}",
            request.SessionId,
            createTimeout.TotalSeconds);
        var run = await _runtimeProvider.RunAsync(
            new IsolatedRuntimeSpec
            {
                Image = image,
                Name = $"ncf-sandbox-{request.SessionId}",
                Detached = true,
                ReadOnlyRootFileSystem = false,
                DropAllCapabilities = true,
                NoNewPrivileges = true,
                NetworkMode = string.Empty,
                CpuLimit = request.CpuLimit,
                MemoryMegabytes = request.MemoryMb,
                PidsLimit = 256,
                Timeout = createTimeout,
                Labels = CreateLabels(request.SessionId),
                Environment = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["JUPYTER_TOKEN"] = token
                },
                Ports = ports,
                Mounts =
                [
                    new IsolatedRuntimeMount(
                        Path.GetFullPath(request.WorkspaceDirectory),
                        "/home/jovyan/work")
                ],
                Command =
                [
                    "start-notebook.py",
                    $"--ServerApp.token={token}",
                    $"--ServerApp.base_url={baseUrl}",
                    "--ServerApp.allow_origin=*",
                    "--ServerApp.allow_remote_access=True",
                    "--ServerApp.disable_check_xsrf=True",
                    "--ServerApp.root_dir=/home/jovyan/work"
                ]
            },
            cancellationToken).ConfigureAwait(false);
        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Docker 启动失败: {run.StandardError}\n{run.StandardOutput}");
        }

        var containerId = run.StandardOutput.Trim();
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
            Message = "JupyterLab 已启动；请使用本机映射端口打开。",
            ExtraPorts = resolvedExtraPorts.Count == 0
                ? null
                : string.Join(";", resolvedExtraPorts.Select(z => z.ToDisplayString()))
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

        var run = await _runtimeProvider.RunAsync(
                new IsolatedRuntimeSpec
                {
                    Image = image,
                    Name = $"ncf-sandbox-{request.SessionId}",
                    Detached = true,
                    ReadOnlyRootFileSystem = true,
                    DropAllCapabilities = true,
                    NoNewPrivileges = true,
                    NetworkMode = network,
                    WorkingDirectory = "/workspace",
                    CpuLimit = request.CpuLimit,
                    MemoryMegabytes = request.MemoryMb,
                    PidsLimit = 256,
                    Timeout = TimeSpan.FromSeconds(
                        Math.Clamp(preview.StartupTimeoutSeconds + 30, 60, 600)),
                    Labels = CreateLabels(request.SessionId),
                    Environment = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["HOME"] = "/tmp/home",
                        ["DOTNET_CLI_HOME"] = "/tmp/dotnet-home",
                        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                        ["NCF_SOLUTION_RELATIVE_PATH"] =
                            preview.SolutionRelativePath.Replace('\\', '/'),
                        ["NCF_XNCF_PREVIEW_PATH_BASE"] = previewBasePath
                    },
                    Ports =
                    [
                        new IsolatedRuntimePortBinding(
                            "127.0.0.1",
                            hostPort,
                            request.Template.ContainerPort)
                    ],
                    Mounts =
                    [
                        new IsolatedRuntimeMount(
                            Path.GetFullPath(request.WorkspaceDirectory),
                            "/workspace")
                    ],
                    TmpfsMounts =
                    [
                        new IsolatedRuntimeTmpfsMount(
                            "/tmp",
                            "rw,noexec,nosuid,size=256m")
                    ],
                    Command = ["sh", "-c", NcfPreviewLaunchScript]
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Docker NCF 预览启动失败: {run.StandardError}\n{run.StandardOutput}");
        }

        var containerId = run.StandardOutput.Trim();
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
            var logs = await _runtimeProvider
                .GetLogsAsync(containerId, 160, CancellationToken.None)
                .ConfigureAwait(false);
            await DestroyAsync(containerId, CancellationToken.None).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"NCF Sandbox 预览未在规定时间内监听端口。\n{logs}");
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
        var environment = new Dictionary<string, string>(StringComparer.Ordinal);
        var workingDirectory = string.Empty;
        IsolatedRuntimeTmpfsMount[] tmpfs =
        [
            new("/tmp", "rw,noexec,nosuid,size=128m")
        ];

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
            environment = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["HOME"] = "/tmp",
                ["DOTNET_CLI_HOME"] = "/tmp/dotnet-home",
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
            };
            workingDirectory = "/work";
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

        var run = await _runtimeProvider.RunAsync(
            new IsolatedRuntimeSpec
            {
                Image = image,
                AutoRemove = true,
                NetworkMode = "none",
                ReadOnlyRootFileSystem = true,
                DropAllCapabilities = true,
                NoNewPrivileges = true,
                CpuLimit = request.CpuLimit,
                MemoryMegabytes = request.MemoryMb,
                PidsLimit = 128,
                Timeout = request.Timeout,
                Labels = CreateLabels(request.SessionId),
                Environment = environment,
                WorkingDirectory = workingDirectory,
                TmpfsMounts = tmpfs,
                Mounts =
                [
                    new IsolatedRuntimeMount(
                        Path.GetFullPath(workDir),
                        "/work",
                        mountReadOnly)
                ],
                Command = command
            },
            cancellationToken).ConfigureAwait(false);
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
            StdOut = run.StandardOutput,
            StdErr = run.StandardError
        };
    }

    public async Task<SandboxExecResult> ExecInteractiveAsync(
        SandboxInteractiveExecRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RuntimeHandle))
        {
            throw new InvalidOperationException("交互式 Sandbox 缺少运行时句柄。");
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            throw new InvalidOperationException("交互式 Sandbox 命令不能为空。");
        }

        var timeout = request.Timeout <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(30)
            : request.Timeout;
        var maxOutputCharacters = request.MaxOutputCharacters <= 0
            ? 32_000
            : request.MaxOutputCharacters;
        var hasStdin = !string.IsNullOrEmpty(request.StdinContent);

        _logger.LogInformation(
            "Sandbox interactive command starting: session={SessionId} container={RuntimeHandle} workdir={WorkingDirectory} stdin={HasStdin}",
            request.SessionId,
            request.RuntimeHandle,
            request.WorkingDirectory,
            hasStdin);

        var run = await _runtimeProvider.ExecAsync(
                new IsolatedRuntimeExecRequest(
                    request.RuntimeHandle,
                    ["/bin/sh", "-lc", request.Command],
                    request.WorkingDirectory,
                    hasStdin ? request.StdinContent : null,
                    timeout,
                    maxOutputCharacters),
                cancellationToken)
            .ConfigureAwait(false);

        return new SandboxExecResult
        {
            ExitCode = run.ExitCode,
            StdOut = run.StandardOutput,
            StdErr = run.StandardError
        };
    }

    public async Task DestroyAsync(string runtimeHandle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runtimeHandle))
        {
            return;
        }

        await _runtimeProvider
            .RemoveAsync(runtimeHandle, force: true, cancellationToken)
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
        return await _runtimeProvider.ListHandlesAsync(
            new IsolatedRuntimeListRequest(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["ncf.sandbox"] = "1"
                },
                IncludeStopped: all),
            cancellationToken).ConfigureAwait(false);
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

    private static IReadOnlyList<SandboxPortMapping> ResolveExtraPortMappings(
        IReadOnlyList<SandboxPortMapping>? extraPortMappings,
        int mainContainerPort,
        int mainHostPort)
    {
        if (extraPortMappings is not { Count: > 0 })
        {
            return Array.Empty<SandboxPortMapping>();
        }

        var takenHostPorts = new HashSet<int> { mainHostPort };
        var resolved = new List<SandboxPortMapping>();
        foreach (var mapping in extraPortMappings)
        {
            var hostPort = mapping.HostPort;
            if (hostPort == 0)
            {
                hostPort = GetFreeTcpPortExcluding(takenHostPorts);
            }
            else if (!takenHostPorts.Add(hostPort))
            {
                throw new InvalidOperationException($"宿主端口已被占用或重复：{hostPort}");
            }

            resolved.Add(mapping with { HostPort = hostPort });
        }

        return resolved;
    }

    private static int GetFreeTcpPortExcluding(HashSet<int> taken)
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var port = GetFreeTcpPort();
            if (taken.Add(port))
            {
                return port;
            }
        }

        throw new InvalidOperationException("无法为附加端口分配空闲宿主端口，请显式指定 hostPort:containerPort。");
    }

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

    private static IReadOnlyDictionary<string, string> CreateLabels(string sessionId)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ncf.sandbox"] = "1",
            ["ncf.sandbox.session"] = sessionId
        };
    }
}
