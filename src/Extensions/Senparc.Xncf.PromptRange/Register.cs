using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using AutoMapper;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.PromptRange";

        public override string Uid => "C6175B8E-9F79-4053-9523-F8E4AC0C3E18"; //必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.5.2"; //必须填写版本号

        public override string MenuName => "提示词靶场";

        public override string Icon => "fa fa-dot-circle-o";

        public override string Description => "你的提示词（Prompt）训练场";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider,
            InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //根据安装或更新不同条件执行逻辑
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //新安装

                    #region 初始化数据库数据

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
            PromptRangeSenparcEntities mySenparcEntities =
                serviceProvider.GetService(mySenparcEntitiesType) as PromptRangeSenparcEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion

            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion

        private static SenparcAiSetting SenparcAiSetting { get; set; }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            registerService.UseSenparcAI(SenparcAiSetting);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });
            return base.UseXncfModule(app, registerService);
        }

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment env)
        {
            //services.AddScoped<Color>();
            //services.AddScoped<ColorDto>();
            //services.AddScoped<ColorService>();
            services.AddScoped<PromptService>();
            services.AddScoped<PromptItemService>();
            services.AddScoped<PromptResultService>();
            services.AddScoped<LlmModelService>();

            //services.AddScoped<IAiHandler>(s => new SemanticAiHandler());

            SenparcAiSetting = SenparcAiSetting ?? new SenparcAiSetting();
            configuration.GetSection("SenparcAiSetting").Bind(SenparcAiSetting);

            services.AddAutoMapper(z =>
            {
                z.CreateMap<PromptItem, PromptItemDto>();
                z.CreateMap<PromptItemDto, PromptItem>();
                z.CreateMap<PromptResult, PromptResultDto>();
                z.CreateMap<PromptResultDto, PromptResult>();

                //TODO:morek
            });

            return base.AddXncfModule(services, configuration, env);
        }
    }
}