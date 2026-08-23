/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxNcfPreviewWorkloadService.cs
    文件功能描述：固定函数 NCF/XNCF Sandbox 预览工作负载

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱工作负载

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

----------------------------------------------------------------*/

using Microsoft.Extensions.Options;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// The Builder can only provide a sanitized snapshot and a validated relative solution path. This
/// service makes a second, Sandbox-owned copy before the container starts; no production checkout
/// is mounted into Docker and no arbitrary command is accepted.
/// </summary>
public sealed class SandboxNcfPreviewWorkloadService : IXncfSandboxPreviewService
{
    private const long MaxWorkspaceBytes = 768L * 1024 * 1024;

    private readonly SandboxOrchestrator _orchestrator;
    private readonly ISandboxImageResolver _imageResolver;
    private readonly SandboxNcfPreviewOptions _options;

    public SandboxNcfPreviewWorkloadService(
        SandboxOrchestrator orchestrator,
        ISandboxImageResolver imageResolver,
        IOptions<SandboxNcfPreviewOptions> options)
    {
        _orchestrator = orchestrator;
        _imageResolver = imageResolver;
        _options = options?.Value ?? new SandboxNcfPreviewOptions();
    }

    public async Task<XncfSandboxPreviewInfo> StartAsync(
        XncfSandboxPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_options.Enabled)
        {
            throw new InvalidOperationException(
                "Sandbox NCF 预览默认关闭。管理员须先启用 SenparcXncfSandbox:NcfPreview:Enabled，并配置不可变的受信任镜像 digest。");
        }
        if (!Directory.Exists(request.SourceWorkspacePath))
        {
            throw new DirectoryNotFoundException("隔离源码工作区不存在，Sandbox 不会接受目标源码路径。");
        }
        ValidateRelativePath(request.SolutionRelativePath, nameof(request.SolutionRelativePath));
        ValidateModuleName(request.ModuleProjectName);
        if (!File.Exists(Path.Combine(request.SourceWorkspacePath, request.SolutionRelativePath)))
        {
            throw new FileNotFoundException("Sandbox 工作区中未找到指定解决方案。", request.SolutionRelativePath);
        }
        if (!SandboxTemplateCatalog.TryGet(SandboxTemplateKeys.NcfPreview, out var template))
        {
            throw new InvalidOperationException("Sandbox 未注册 NCF 预览模板。");
        }

        var image = _imageResolver.Resolve(template.Key, template.Image);
        if (!image.Contains("@sha256:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "请在 SenparcXncfSandbox:Images:Overrides:ncf-preview 配置镜像 digest；标签（tag）不能作为不可信代码预览的镜像身份。");
        }

        if (_options.AllowDependencyRestoreNetwork && string.IsNullOrWhiteSpace(_options.RestoreNetworkName))
        {
            throw new InvalidOperationException("已启用依赖还原网络，但未配置专用包镜像 Docker 网络。");
        }

        // If the administrator leaves the option disabled, the workload still runs with
        // --network none and can use only dependencies already embedded in the approved image.
        // This makes the secure default useful while retaining an explicit path for package-mirror
        // based restores in configured deployments.
        var allowRestoreNetwork = request.AllowDependencyRestoreNetwork
                                  && _options.AllowDependencyRestoreNetwork;

        var result = await _orchestrator.CreateAsync(
                request.OwnerUserId,
                SandboxTemplateKeys.NcfPreview,
                SandboxRuntimeKind.Docker,
                initializeWorkspace: (destination, token) => CopyWorkspaceAsync(request.SourceWorkspacePath, destination, token),
                ncfPreview: new SandboxNcfPreviewRuntimeOptions
                {
                    SolutionRelativePath = request.SolutionRelativePath,
                    ModuleProjectName = request.ModuleProjectName,
                    // Docker derives the final path from its server-generated session id.
                    BasePath = string.Empty,
                    AllowDependencyRestoreNetwork = allowRestoreNetwork,
                    RestoreNetworkName = _options.RestoreNetworkName,
                    StartupTimeoutSeconds = Math.Clamp(_options.StartupTimeoutSeconds, 30, 600)
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // CreateAsync allocates the session id. The runtime options need the exact path-base, so
        // the orchestrator replaces the placeholder after it has generated the id (see below).
        return new XncfSandboxPreviewInfo
        {
            SandboxSessionId = result.SessionId,
            Status = result.Status,
            AccessUrl = result.AccessUrl,
            StatusMessage = result.StatusMessage
        };
    }

    public Task StopAsync(string sandboxSessionId, CancellationToken cancellationToken = default) =>
        _orchestrator.DestroyAsync(sandboxSessionId, cancellationToken);

    private static async Task CopyWorkspaceAsync(string sourceRoot, string destinationRoot, CancellationToken cancellationToken)
    {
        var source = Path.GetFullPath(sourceRoot);
        var destination = Path.GetFullPath(destinationRoot);
        Directory.CreateDirectory(destination);
        long copiedBytes = 0;
        var pending = new Stack<(string Source, string Relative)>();
        pending.Push((source, string.Empty));

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (current, relative) = pending.Pop();
            var currentInfo = new DirectoryInfo(current);
            if (currentInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new UnauthorizedAccessException("Sandbox 拒绝复制符号链接目录。");
            }

            foreach (var directory in Directory.EnumerateDirectories(current))
            {
                var info = new DirectoryInfo(directory);
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new UnauthorizedAccessException("Sandbox 拒绝复制符号链接目录。");
                }
                pending.Push((info.FullName, Path.Combine(relative, info.Name)));
            }

            foreach (var filePath in Directory.EnumerateFiles(current))
            {
                var info = new FileInfo(filePath);
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new UnauthorizedAccessException("Sandbox 拒绝复制符号链接文件。");
                }
                copiedBytes += info.Length;
                if (copiedBytes > MaxWorkspaceBytes)
                {
                    throw new InvalidOperationException("Sandbox 工作区超过 768 MB 安全上限。");
                }

                var target = Path.Combine(destination, relative, info.Name);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                await using var input = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await input.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void ValidateRelativePath(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || Path.IsPathRooted(value)
            || value.Contains("..", StringComparison.Ordinal)
            || value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
        {
            throw new ArgumentException("只允许 Sandbox 工作区内的相对解决方案路径。", parameterName);
        }
    }

    private static void ValidateModuleName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || value.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')))
        {
            throw new ArgumentException("模块项目名称无效。", nameof(value));
        }
    }
}
