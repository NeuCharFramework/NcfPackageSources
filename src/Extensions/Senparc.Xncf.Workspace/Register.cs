/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Register.cs
    File description: Workspace module registration
    
    
    Created by: Senparc - 20261015
    
    Modified by: Senparc - 20261021
    Change description: v0.1.1 register metadata storage and migrate the database on installation

    Modified by: Senparc - 20261028
    Change description: register IHttpContextAccessor so singleton storage can capture request tenant context

    Modified by: Senparc - 20261103
    Change description: v0.2.2 restore request tenant context for singleton Workspace metadata access

    Modified by: Senparc - 20261109
    Change description: register administrator-isolated Workspace usage directories

    Modified by: Senparc - 20261111
    Change description: v0.2.7 unify XNCF Register display metadata and localization keys

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
    private static readonly XncfRegisterText RegisterText =
        XncfRegisterText.Create<WorkspaceResource>(
            "Module.Workspace",
            "Workspace",
            "Provides isolated file access, desktop editing, and secure runtime support through a cross-platform Workspace Agent.");

    public override string Name => "Senparc.Xncf.Workspace";
    public override string Uid => "9D0AA0B5-1E76-4BA3-96A0-4BE1A8D6A6E7";
    public override string Version => "0.2.2";
    public override string MenuName => RegisterText.MenuName;
    public override string Icon => "fa fa-folder-open";
    public override string Description => RegisterText.Description;

    public override IServiceCollection AddXncfModule(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.Configure<WorkspaceOptions>(
            configuration.GetSection(WorkspaceOptions.SectionName));
        services.AddHttpContextAccessor();
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
        services.AddScoped<IWorkspaceCatalog, WorkspaceCatalog>();
        return base.AddXncfModule(services, configuration, env);
    }

    public override Task InstallOrUpdateAsync(
        IServiceProvider serviceProvider,
        InstallOrUpdate installOrUpdate)
        => XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
}
