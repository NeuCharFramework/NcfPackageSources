/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Setup.cshtml.cs
    File description: Senparc.Xncf.Sandbox.Areas.Sandbox.Pages related implementation

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;
using System.Globalization;

namespace Senparc.Xncf.Sandbox.Areas.Sandbox.Pages;

public class Setup : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private readonly IEnumerable<ISandboxRuntime> _runtimes;
    private readonly ISandboxImageResolver _imageResolver;

    /// <summary>
    /// Initializes the Sandbox environment setup page.
    /// </summary>
    /// <param name="xncfModuleService">The lazy XNCF module service.</param>
    /// <param name="runtimes">The registered Sandbox runtimes.</param>
    /// <param name="imageResolver">The configured image resolver.</param>
    public Setup(
        Lazy<XncfModuleService> xncfModuleService,
        IEnumerable<ISandboxRuntime> runtimes,
        ISandboxImageResolver imageResolver)
        : base(xncfModuleService)
    {
        _runtimes = runtimes;
        _imageResolver = imageResolver;
    }

    public bool DockerAvailable { get; private set; }

    public string DockerStatusMessage { get; private set; } = string.Empty;

    public string DocsUrl { get; private set; } = SandboxDocsLinks.EnvironmentSetupZh;

    public bool HasRegistryPrefix { get; private set; }

    public string? RegistryPrefix { get; private set; }

    /// <summary>
    /// Loads documentation/image configuration and probes Docker availability.
    /// </summary>
    /// <returns>A task that completes after environment probing.</returns>
    public async Task OnGetAsync()
    {
        DocsUrl = SandboxDocsLinks.GetEnvironmentSetupUrl(CultureInfo.CurrentUICulture.Name);
        HasRegistryPrefix = _imageResolver.HasRegistryPrefix;
        RegistryPrefix = _imageResolver.RegistryPrefix;
        await ProbeDockerAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Probes Docker availability and returns localized setup state.
    /// </summary>
    /// <returns>An HTTP 200 JSON result containing probe state.</returns>
    public async Task<IActionResult> OnGetProbeAsync()
    {
        await ProbeDockerAsync().ConfigureAwait(false);
        return new JsonResult(new
        {
            success = true,
            dockerAvailable = DockerAvailable,
            message = DockerStatusMessage,
            docsUrl = SandboxDocsLinks.GetEnvironmentSetupUrl(CultureInfo.CurrentUICulture.Name),
            hasRegistryPrefix = _imageResolver.HasRegistryPrefix,
            registryPrefix = _imageResolver.RegistryPrefix
        });
    }

    /// <summary>
    /// Probes the registered Docker runtime and stores a user-facing status message.
    /// </summary>
    /// <returns>A task that completes after probing.</returns>
    private async Task ProbeDockerAsync()
    {
        var docker = _runtimes.FirstOrDefault(z => z.Kind == SandboxRuntimeKind.Docker);
        if (docker == null)
        {
            DockerAvailable = false;
            DockerStatusMessage = "未注册 Docker 运行时。";
            return;
        }

        try
        {
            DockerAvailable = await docker.IsAvailableAsync().ConfigureAwait(false);
            DockerStatusMessage = DockerAvailable
                ? "已检测到可用的 Docker（或兼容 CLI）daemon。"
                : "未检测到可用的 Docker。请按官方文档手动安装并启动后重试检测。";
        }
        catch (Exception ex)
        {
            DockerAvailable = false;
            DockerStatusMessage = "Docker 检测异常：" + ex.Message;
        }
    }
}
