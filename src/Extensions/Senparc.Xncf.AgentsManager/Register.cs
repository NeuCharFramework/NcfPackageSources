using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
using Senparc.Xncf.XncfBuilder.OHS.Local;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.AgentsManager";

        public override string Uid => "D858D7FA-775A-4690-9023-CFB0B3B84994";//It must be globally unique and must be fixed after generation. It has been automatically generated and can also be modified by yourself.

        public override string Version => "0.3.18.9";//Version number is required

        public override string MenuName => "Agents 管理模块";

        public override string Icon => "fa fa-star";

        public override string Description => "Agents 管理模块";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //Update database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //Execute logic based on different conditions for installation or update
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //New installation
                    #region 初始化数据库数据
                    //var colorService = serviceProvider.GetService<ColorAppService>();
                    //var colorResult = await colorService.GetOrInitColorAsync();
                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //renew
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

            //Specify the data entity to be deleted

            //Note: As a demonstration, all tables created by this module are deleted when uninstalling the module. During actual operation, please operate with caution and sort the entities in the order of deletion!
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
                profile.CreateMap<ChatGroupHistory, ChatGroupHistoryDto>().ReverseMap();
                profile.CreateMap<ChatTask, ChatTaskDto>().ReverseMap();
            });

            //Service DI
            services.AddScoped<AgentsTemplateService>();
            services.AddSingleton<PromptOptimizationAgentBridge>();
            services.AddScoped<PromptOptimizationKernelFallbackService>();
            services.AddScoped<PromptOptimizationService>(); // Register PromptOptimizationService
            services.AddScoped<ChatGroupService>();
            services.AddScoped<ChatGroupHistoryService>();
            services.AddScoped<ChatTaskService>();
            services.AddScoped<ChatGroupMemberService>();

            //AI Plugins DI
            services.AddScoped<PromptCatalyzerPlugin>();
            services.AddScoped<PromptOptimizationPlugin>();  // 🔥 Newly added: Prompt optimization Plugin (including GetPromptInfo, CreateOptimizedPrompt, ExecuteShootTest, ExecuteAIGrade and other methods)
            services.AddScoped<CrawlPlugin>();
            services.AddScoped<FormatorPlugin>();
            services.AddScoped<TranslatorPlugin>();

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
            aiPlugins.Add(typeof(PromptOptimizationPlugin));  // 🔥 New: Prompt Optimization Plugin
            aiPlugins.Add(typeof(CrawlPlugin));
            aiPlugins.Add(typeof(FormatorPlugin));
            aiPlugins.Add(typeof(TranslatorPlugin));

            return base.UseXncfModule(app, registerService);
        }
    }
}
















