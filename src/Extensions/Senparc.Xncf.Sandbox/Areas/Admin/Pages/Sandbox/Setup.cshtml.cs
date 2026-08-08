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

    public async Task OnGetAsync()
    {
        DocsUrl = SandboxDocsLinks.GetEnvironmentSetupUrl(CultureInfo.CurrentUICulture.Name);
        HasRegistryPrefix = _imageResolver.HasRegistryPrefix;
        RegistryPrefix = _imageResolver.RegistryPrefix;
        await ProbeDockerAsync().ConfigureAwait(false);
    }

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
