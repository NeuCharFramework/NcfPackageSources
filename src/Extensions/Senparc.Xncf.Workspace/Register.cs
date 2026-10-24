/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：Workspace 模块注册
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.1 注册元数据存储，并在安装时迁移数据库

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.IsolatedRuntime;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Xncf.Workspace.Domain.Services;

namespace Senparc.Xncf.Workspace;

[XncfRegister]
public sealed partial class Register : XncfRegisterBase, IXncfRegister
{
    public override string Name => "Senparc.Xncf.Workspace";
    public override string Uid => "9D0AA0B5-1E76-4BA3-96A0-4BE1A8D6A6E7";
    public override string Version => "0.2.2";
    public override string MenuName => "Workspace";
    public override string Icon => "fa fa-folder-open";
    public override string Description => "通过跨平台 Workspace Agent 提供隔离文件访问、桌面编辑和安全运行时。";

    public override IServiceCollection AddXncfModule(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.Configure<WorkspaceOptions>(
            configuration.GetSection(WorkspaceOptions.SectionName));
        services.AddDockerIsolatedRuntime(configuration);
        services.AddScoped<Domain.Models.DatabaseModel.WorkspaceSenparcEntities>(serviceProvider =>
            (Domain.Models.DatabaseModel.WorkspaceSenparcEntities)serviceProvider.GetRequiredService(
                TryGetXncfDatabaseDbContextType));
        services.AddSingleton<WorkspaceAgentProcessFactory>();
        services.AddSingleton<WorkspaceAgentConnectionPool>();
        services.AddSingleton<WorkspaceChangeHub>();
        services.AddSingleton<IWorkspaceStorage, WorkspaceStorage>();
        services.AddSingleton<IWorkspaceClientFactory, WorkspaceClientFactory>();
        services.AddScoped<IWorkspaceMetadataStore, WorkspaceMetadataStore>();
        return base.AddXncfModule(services, configuration, env);
    }

    public override Task InstallOrUpdateAsync(
        IServiceProvider serviceProvider,
        InstallOrUpdate installOrUpdate)
        => XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
}
