/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance Workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support Human Input node suspension and external resumption

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance Workflow function calls, task control, and replay management

    Modified by: Senparc - 20260829
    Change description: v0.3.0 add Workflow analytics queries and the Admin visualization

    Modified by: Senparc - 20260909
    Change description: v0.4.0 add the Chat trigger, chat page, session service, and anonymous access

    Modified by: Senparc - 20260913
    Change description: v0.4.0 register Chat message repositories and service dependencies

    Modified by: Senparc - 20260915
    Change description: v0.4.0 enhance Chat trigger message persistence and recovery

    Modified by: Senparc - 20261111
    Change description: v0.4.6 unify XNCF Register display metadata and localization keys

----------------------------------------------------------------*/

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
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow;

[XncfRegister]
[XncfOrder(5890)]
public partial class Register : XncfRegisterBase, IXncfRegister
{
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<NeuCharWorkflowResource>(
            "Module",
            "NeuChar Workflow",
            "Secure server-side workflows composed of Functions, system nodes, and controlled Agents.");

    public override string Name => "Senparc.Xncf.NeuCharWorkflow";

    public override string Uid => "5F972F29-A2A5-4A87-B5B8-2FE758AA457B";

    public override string Version => "0.1.0-preview1";

    public override string MenuName => RegisterText.MenuName;

    public override string Icon => "fa fa-random";

    public override string Description => RegisterText.Description;

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
        services.AddScoped<INeuCharWorkflowChatMessageRepository, NeuCharWorkflowChatMessageRepository>();
        services.AddScoped<NeuCharWorkflowService>();
        services.AddScoped<NeuCharWorkflowVersionService>();
        services.AddScoped<NeuCharWorkflowExecutionLogService>();
        services.AddScoped<NeuCharWorkflowChatMessageService>();
        services.AddScoped<NeuCharWorkflowFunctionService>();
        services.AddScoped<NeuCharWorkflowAnalyticsService>();
        services.AddDataProtection();
        services.AddScoped<NeuCharWorkflowParameterProtector>();
        services.AddSingleton<NeuCharWorkflowNeuBellProvider>();
        services.AddSingleton<INeuBellProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<NeuCharWorkflowNeuBellProvider>());
        services.AddSingleton<NeuCharWorkflowHumanInputService>();
        services.AddSingleton<NeuCharWorkflowChatSessionService>();
        services.AddScoped<NeuCharWorkflowEngine>();
        services.AddSingleton<NeuCharWorkflowRunCoordinator>();
        // Register the null bridge only when no external execution module provides one.
        // AgentsManager must become the final HIL implementation regardless of registration order.
        if (!services.Any(descriptor =>
                descriptor.ServiceType == typeof(IWorkflowHumanInteractionBridge)))
        {
            services.AddSingleton<IWorkflowHumanInteractionBridge, NullWorkflowHumanInteractionBridge>();
        }
        services.AddScoped<WorkflowEventPublisher>();
        services.AddScoped<NeuCharWorkflowAppService>();
        services.AddScoped<NeuCharWorkflowFunctionCallingProvider>();
        services.AddScoped<IWorkflowFunctionCallingProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<NeuCharWorkflowFunctionCallingProvider>());
        services.AddScoped<NeuCharWorkflowCreationProvider>();
        services.AddScoped<IWorkflowCreationProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<NeuCharWorkflowCreationProvider>());
        services.AddScoped<IWorkflowDependencyProvider, NeuCharWorkflowDependencyProvider>();
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
