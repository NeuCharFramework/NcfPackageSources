using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Senparc.AI.Kernel;
using Senparc.CO2NET.RegisterServices;
using System.Reflection;
using Senparc.AI.Interfaces;
using Senparc.Xncf.AIKernel.AutoMapperProfiles;

namespace Senparc.Xncf.AIKernel
{
    [XncfRegister]
    //[XncfOrder(5899)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.AIKernel";

        public override string Uid => "796D12D8-580B-40F3-A6E8-A5D9D2EABB69";//It must be globally unique and must be fixed after generation. It has been automatically generated and can also be modified by yourself.

        public override string Version => "5.0.5";//Version number is required

        public override string MenuName => "AI 核心模块";

        public override string Icon => "fa  fa-magic";

        public override string Description => "AI 核心模块，基于 Senparc.AI 为所有 AI 项目提供基础能力";

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

                    //TODO: Automatically pull NeuChar free usage for configuration and load Seed data
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
            AIKernelSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AIKernelSenparcEntities;

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
            services.AddSenparcAI(configuration);
            //services.AddScoped<ISenparcAiSetting, SenparcAiSetting>();
            //Console.WriteLine("======================services.AddScoped<ISenparcAiSetting, SenparcAiSetting>();================");
            services.AddScoped<SemanticAiHandler>();

            services.AddAutoMapper(config =>
            {
                config.AddProfile<AIKernelAutoMapperProfile>();
            });

            return base.AddXncfModule(services, configuration, env);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            registerService.UseSenparcAI();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });
            return base.UseXncfModule(app, registerService);
        }
    }
}






