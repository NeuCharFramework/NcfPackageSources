using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace;

[XncfRegister]
public sealed class Register : XncfRegisterBase, IXncfRegister
{
    public override string Name => "Senparc.Xncf.Workspace";
    public override string Uid => "9D0AA0B5-1E76-4BA3-96A0-4BE1A8D6A6E7";
    public override string Version => "0.1.0";
    public override string MenuName => "Workspace";
    public override string Icon => "fa fa-folder-open";
    public override string Description => "隔离的 AI 工作区文件系统。";

    public override IServiceCollection AddXncfModule(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.Configure<WorkspaceOptions>(
            configuration.GetSection(WorkspaceOptions.SectionName));
        services.AddSingleton<IWorkspaceStorage, WorkspaceStorage>();
        return base.AddXncfModule(services, configuration, env);
    }

    public override Task InstallOrUpdateAsync(
        IServiceProvider serviceProvider,
        InstallOrUpdate installOrUpdate) => Task.CompletedTask;
}
