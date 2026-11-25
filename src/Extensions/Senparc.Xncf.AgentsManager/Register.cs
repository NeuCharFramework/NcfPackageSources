/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: module registration and initialization logic

    Created by: Senparc - 20200818

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260704
    Change description: v0.11.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260717
    Change description: v0.12.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.15.0-preview20 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.19.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 add SourceFileId, SourceType, and SourceUrl columns to PromptRange table for PostgreSQL, SQL Server, and SQLite

    Modified by: Senparc - 20261111
    Change description: v0.19.3 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261124
    Change description: v0.19.4 Integrated localization, tooling, and runtime improvements across the module.

----------------------------------------------------------------*/

using A2A.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins;
using Senparc.Xncf.AgentsManager.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.AgentsManager.Abstractions;
using Senparc.Xncf.XncfBuilder.OHS.Local;
using Senparc.Ncf.Core.Localization;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.AgentsManager.OHS.Local.AppService;
using Senparc.Ncf.Shared.Abstractions.ChatAgent;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister, IChatTool
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<AgentsManagerResource>(
            "Module.AgentsManager",
            "Agents 管理模块",
            "管理 AI 智能体及相关配置");

        public const string ModuleUid = "D858D7FA-775A-4690-9023-CFB0B3B84994";

        #region Legacy section

        public override string Name => "Senparc.Xncf.AgentsManager";

        public override string Uid => ModuleUid;//Legacy comment translated to English.

        public override string Version => "0.3.23";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-star";

        public override string Description => RegisterText.Description;

        public IReadOnlyCollection<ChatToolDefinition> GetChatTools()
        {
            return new[]
            {
                new ChatToolDefinition
                {
                    Command = "/run-agent-group",
                    Label = AgentsManagerResource.Get(
                        "AdminChat.Command.RunAgentGroup",
                        "Run an Agent group"),
                    Hint = AgentsManagerResource.Get(
                        "AdminChat.Command.RunAgentGroupHint",
                        "Describe the task; reuse an existing matching group"),
                    ModuleName = MenuName,
                    ModuleUid = Uid,
                    Functions = new[]
                    {
                        new ChatToolFunctionDefinition
                        {
                            DeclaringType = typeof(ChatGroupAppService),
                            MethodName = nameof(ChatGroupAppService.FindChatGroups)
                        },
                        new ChatToolFunctionDefinition
                        {
                            DeclaringType = typeof(ChatGroupAppService),
                            MethodName = nameof(ChatGroupAppService.RunChatGroup)
                        }
                    },
                    Instructions = """

[Agent Group Command]
The user asked to run an existing AgentsManager ChatGroup. Do not create, edit, or delete a group.
1. Search enabled groups with FindChatGroups using task keywords, group name, or the explicit --group value.
2. Reuse an exact ID when supplied. For a semantic match, choose only when one group clearly matches; otherwise show the closest options and ask the user to choose.
3. Start the selected existing group with RunChatGroup and the user's task as Command.
4. Report the selected group and the resulting task status; do not claim completion because the run is asynchronous.
"""
                }
            };
        }

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //update the database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //execute logic according to the installation or update condition
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //new installation
                    #region Legacy section
                    //var colorService = serviceProvider.GetService<ColorAppService>();
                    //var colorResult = await colorService.GetOrInitColorAsync();
                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //update
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AgentsManagerSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AgentsManagerSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //AutoMap mapping
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<AgentTemplate, AgentTemplateDto>().ReverseMap();
                profile.CreateMap<AgentTemplate, AgentTemplateSimpleStatusDto>().ReverseMap();
                profile.CreateMap<ChatGroup, ChatGroupDto>().ReverseMap();
                profile.CreateMap<ChatGroupMember, ChatGroupMemberDto>().ReverseMap();
                profile.CreateMap<RemoteAgent, RemoteAgentDto>().ReverseMap();
                profile.CreateMap<ChatGroupRemoteMember, ChatGroupRemoteMemberDto>().ReverseMap();
                profile.CreateMap<PublishedA2AAgent, PublishedA2AAgentDto>().ReverseMap();
                profile.CreateMap<ChatGroupHistory, ChatGroupHistoryDto>().ReverseMap();
                profile.CreateMap<ChatTask, ChatTaskDto>().ReverseMap();
            });

            //Service DI
            services.AddScoped<AgentsTemplateService>();
            services.AddSingleton<PromptOptimizationAgentBridge>();
            services.AddScoped<PromptOptimizationKernelFallbackService>();
            services.AddScoped<PromptOptimizationService>(); //Legacy comment translated to English.
            services.AddSingleton<ChatTaskStreamHub>();
            services.AddSingleton<ChatTaskCancellationRegistry>();
            services.AddSingleton<HumanInTheLoopRequestStore>();
            services.AddSingleton<AgentsManagerNeuBellProvider>();
            services.AddSingleton<AgentsManagerHumanInteractionService>();
            services.AddSingleton<IWorkflowHumanInteractionBridge, AgentsManagerWorkflowHumanInteractionBridge>();
            services.AddSingleton<INeuBellProvider>(serviceProvider =>
                serviceProvider.GetRequiredService<AgentsManagerNeuBellProvider>());
            services.AddScoped<ChatGroupService>();
            services.AddScoped<ChatGroupHistoryService>();
            services.AddScoped<ChatTaskService>();
            services.AddScoped<ChatGroupMemberService>();
            services.AddScoped<ChatGroupRemoteMemberService>();
            services.AddScoped<RemoteAgentService>();
            services.AddScoped<PublishedA2AAgentService>();
            services.AddScoped<AgentTemplateRunner>();
            services.AddHttpClient(RemoteA2AAgentFactory.HttpClientName);
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            services.AddSingleton<IHttpMessageHandlerBuilderFilter, RemoteA2AHttpMessageHandlerBuilderFilter>();
            services.AddScoped<RemoteA2AAgentFactory>();
            services.AddHttpContextAccessor();
            services.AddSingleton<PublishedA2AServerRegistry>();
            services.AddSingleton<PublishedA2ARequestHandler>();
            services.AddScoped<PublishedA2AAgentFactory>();
            services.AddScoped<AgentsWorkflowObjectProvider>();
            services.AddScoped<AgentWorkflowReferenceValidator>();
            services.AddScoped<IAgentWorkflowReferenceValidator>(serviceProvider =>
                serviceProvider.GetRequiredService<AgentWorkflowReferenceValidator>());
            services.AddScoped<IWorkflowObjectProvider>(serviceProvider =>
                serviceProvider.GetRequiredService<AgentsWorkflowObjectProvider>());

            //AI Plugins DI
            services.AddScoped<PromptCatalyzerPlugin>();
            services.AddScoped<PromptOptimizationPlugin>();  //Legacy comment translated to English.
            services.AddScoped<CrawlPlugin>();
            services.AddScoped<FormatorPlugin>();
            services.AddScoped<TranslatorPlugin>();
            services.AddScoped<PromptRangeSkillPlugin>();

            //test
            services.AddScoped<BuildXncfAppService>();

            return base.AddXncfModule(services, configuration, env);
        }
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });

            var aiPlugins = AIPluginHub.Instance;
            aiPlugins.Add(typeof(PromptCatalyzerPlugin));
            aiPlugins.Add(typeof(PromptOptimizationPlugin));  // 🔥 add：Prompt optimize Plugin
            aiPlugins.Add(typeof(CrawlPlugin));
            aiPlugins.Add(typeof(FormatorPlugin));
            aiPlugins.Add(typeof(TranslatorPlugin));
            // PromptRangeSkillPlugin is instantiated by AgentTemplateRunner with the
            // Agent's explicit Skill binding set; it must not be globally exposed.

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            app.UseWhen(context =>
            {
                if (!HttpMethods.IsPost(context.Request.Method))
                {
                    return false;
                }

                var segments = context.Request.Path.Value?.Trim('/').Split('/');
                return segments?.Length == 2
                    && string.Equals(segments[0], "a2a", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(segments[1]);
            }, a2aApp =>
            {
                a2aApp.UseRouting();
                a2aApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapA2A(
                        a2aApp.ApplicationServices.GetRequiredService<PublishedA2ARequestHandler>(),
                        "/a2a/{agentKey}");
                });
            });

            return base.UseXncfModule(app, registerService);
        }
    }
}












