/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：模块注册与初始化逻辑

    创建标识：Senparc - 20200818

    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

    修改标识：Senparc - 20260717
    修改描述：v0.12.0-preview6 为 AgentsManager 模块接入统一资源本地化并优化功能文案

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

    修改标识：Senparc - 20260815
    修改描述：v0.15.0-preview20 增强 AgentTemplate、ChatGroup 与发布型 A2A 的取消和请求处理

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 AgentTemplate 模型绑定、空输出 Token 重试与 Human-in-the-Loop

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 增强 Agent 工作流校验、函数绑定与任务管理交互

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
using Senparc.Ncf.Shared.Abstractions.ChatAgent;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        public const string ModuleUid = "D858D7FA-775A-4690-9023-CFB0B3B84994";

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.AgentsManager";

        public override string Uid => ModuleUid;//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.3.22";//必须填写版本号

        public override string MenuName => AgentsManagerResource.Get("Module.AgentsManager.MenuName", "Agents 管理模块");

        public override string Icon => "fa fa-star";

        public override string Description => AgentsManagerResource.Get("Module.AgentsManager.Description", "Agents 管理模块");

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //根据安装或更新不同条件执行逻辑
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //新安装
                    #region 初始化数据库数据
                    //var colorService = serviceProvider.GetService<ColorAppService>();
                    //var colorResult = await colorService.GetOrInitColorAsync();
                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //更新
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AgentsManagerSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AgentsManagerSenparcEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //AutoMap映射
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
                profile.CreateMap<AgentExecutionTask, AgentExecutionTaskDto>().ReverseMap();
            });

            //Service DI
            services.AddScoped<AgentsTemplateService>();
            services.AddSingleton<PromptOptimizationAgentBridge>();
            services.AddScoped<PromptOptimizationKernelFallbackService>();
            services.AddScoped<PromptOptimizationService>(); // 注册 PromptOptimizationService
            services.AddSingleton<ChatTaskStreamHub>();
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
            services.AddScoped<AgentExecutionService>();
            services.AddSingleton<AgentExecutionStreamHub>();
            services.AddSingleton<AgentExecutionRuntimeStore>();
            services.AddHttpClient(RemoteA2AAgentFactory.HttpClientName);
            // Aspire ServiceDefaults 会在所有 HttpClient 上添加固定 30 秒的 Polly 尝试超时。
            // A2A 请求可能需要更长的模型推理时间，并且 POST 不能被隐式重试；
            // 由 Filter 在最终 Handler 链中移除该管道，改用 RemoteAgent.TimeoutSeconds。
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
            services.AddScoped<PromptOptimizationPlugin>();  // 🔥 新增：Prompt 优化 Plugin（含 GetPromptInfo, CreateOptimizedPrompt, ExecuteShootTest, ExecuteAIGrade 等方法）
            services.AddScoped<CrawlPlugin>();
            services.AddScoped<FormatorPlugin>();
            services.AddScoped<TranslatorPlugin>();

            //测试
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
            aiPlugins.Add(typeof(PromptOptimizationPlugin));  // 🔥 新增：Prompt 优化 Plugin
            aiPlugins.Add(typeof(CrawlPlugin));
            aiPlugins.Add(typeof(FormatorPlugin));
            aiPlugins.Add(typeof(TranslatorPlugin));

            // NCF 模块在宿主 UseRouting() 之前初始化。为 A2A JSON-RPC 建立独立分支，
            // 确保其自身先路由再映射端点，而 Agent Card 的 GET 请求仍交给 MVC 控制器。
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














