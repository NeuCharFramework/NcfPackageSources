using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.NeuCharWorkflow.ACL;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using Senparc.Xncf.NeuCharWorkflow.Application.Events;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Models;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow;

[XncfRegister]
[XncfOrder(5890)]
public partial class Register : XncfRegisterBase, IXncfRegister
{
    public override string Name => "Senparc.Xncf.NeuCharWorkflow";

    public override string Uid => "5F972F29-A2A5-4A87-B5B8-2FE758AA457B";

    public override string Version => "0.1.0-preview1";

    public override string MenuName => NeuCharWorkflowResource.Get("Module.MenuName", "NeuChar Workflow");

    public override string Icon => "fa fa-random";

    public override string Description => NeuCharWorkflowResource.Get(
        "Module.Description",
        "Secure server-side workflows composed from Functions, system nodes, and controlled Agents.");

    public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
    {
        await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this).ConfigureAwait(false);
        await serviceProvider.GetRequiredService<LegacyWorkflowMigrationService>()
            .MigrateAsync(installOrUpdate).ConfigureAwait(false);
    }

    public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
    {
        var contextType = TryGetXncfDatabaseDbContextType;
        var context = serviceProvider.GetService(contextType) as NeuCharWorkflowSenparcEntities;
        var tableKeys = EntitySetKeys.GetEntitySetInfo(contextType).Keys.ToArray();
        await base.DropTablesAsync(serviceProvider, context!, tableKeys).ConfigureAwait(false);
        await unsinstallFunc().ConfigureAwait(false);
    }

    public override IServiceCollection AddXncfModule(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.AddScoped<INeuCharWorkflowRepository, NeuCharWorkflowRepository>();
        services.AddScoped<INeuCharWorkflowVersionRepository, NeuCharWorkflowVersionRepository>();
        services.AddScoped<INeuCharWorkflowExecutionLogRepository, NeuCharWorkflowExecutionLogRepository>();
        services.AddScoped<NeuCharWorkflowService>();
        services.AddScoped<NeuCharWorkflowVersionService>();
        services.AddScoped<NeuCharWorkflowExecutionLogService>();
        services.AddScoped<NeuCharWorkflowFunctionService>();
        services.AddDataProtection();
        services.AddScoped<NeuCharWorkflowParameterProtector>();
        services.AddSingleton<NeuCharWorkflowNeuBellProvider>();
        services.AddSingleton<INeuBellProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<NeuCharWorkflowNeuBellProvider>());
        services.AddScoped<NeuCharWorkflowEngine>();
        services.AddSingleton<NeuCharWorkflowRunCoordinator>();
        services.AddScoped<WorkflowEventPublisher>();
        services.AddScoped<NeuCharWorkflowAppService>();
        services.AddSingleton<LegacyWorkflowMigrationService>();
        services.AddHostedService<NeuCharWorkflowHostedService>();
        return base.AddXncfModule(services, configuration, env);
    }

    public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
        });
        return base.UseXncfModule(app, registerService);
    }
}
