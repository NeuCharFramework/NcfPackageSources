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

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

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
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.NeuCharWorkflow.ACL;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using Senparc.Xncf.NeuCharWorkflow.Application.Events;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Models;
using Senparc.Xncf.NeuCharWorkflow.OHS.Local.AppService;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow;

[XncfRegister]
[XncfOrder(5890)]
public partial class Register : XncfRegisterBase, IXncfRegister, IChatTool
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

    public IReadOnlyCollection<ChatToolDefinition> GetChatTools()
    {
        return new[]
        {
            new ChatToolDefinition
            {
                Command = "/run-workflow",
                Label = NeuCharWorkflowResource.Get(
                    "AdminChat.Command.RunWorkflow",
                    "Run a Workflow"),
                Hint = NeuCharWorkflowResource.Get(
                    "AdminChat.Command.RunWorkflowHint",
                    "Describe the task or provide --id=<workflowId>"),
                ModuleName = MenuName,
                ModuleUid = Uid,
                Functions = new[]
                {
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.SearchWorkflows)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.StartWorkflow)
                    }
                },
                Instructions = """

[Workflow Run Command]
The user asked to start an existing NeuChar Workflow owned by the current administrator.
1. Parse an explicit --id value or use SearchWorkflows to find a semantic match by name and description.
2. If more than one candidate is plausible, show the candidates and ask which one to use.
3. Pass the user's remaining text as workflow input and call StartWorkflow exactly once.
4. Report the workflow name and run ID; the workflow runs asynchronously, so do not claim it completed.
5. Do not create, edit, delete, or import a workflow in this command.
"""
            },
            new ChatToolDefinition
            {
                Command = "/edit-workflow",
                Label = NeuCharWorkflowResource.Get(
                    "AdminChat.Command.EditWorkflow",
                    "Assist with Workflow editing"),
                Hint = NeuCharWorkflowResource.Get(
                    "AdminChat.Command.EditWorkflowHint",
                    "Provide --id=<workflowId> and describe the change"),
                ModuleName = MenuName,
                ModuleUid = Uid,
                Functions = new[]
                {
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.SearchWorkflows)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.InspectWorkflow)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.ApplyWorkflowEdit)
                    }
                },
                Instructions = """

[Workflow Editing Assistant]
Help the user edit an existing NeuChar Workflow. Only this command's explicit tools are available.
1. Resolve the workflow by explicit --id and optional --revision, or SearchWorkflows, then call InspectWorkflow.
2. Review the masked graph and the current revision. Never reconstruct protected parameter values.
3. For a clear, concrete request, produce the smallest valid graph change and call ApplyWorkflowEdit with the inspected revision exactly once.
4. If the requested change is ambiguous, or if a revision conflict occurs, stop and ask; do not create another workflow.
5. Preserve masked secret placeholders and existing workflow identity, trigger, enabled state, variables and unrelated nodes.
6. Never call StartWorkflow from this command. After saving, report the new revision and tell the user to reload the editor.
7. Never ask for or synthesize credentials or external-resume secrets.
"""
            }
        };
    }

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
