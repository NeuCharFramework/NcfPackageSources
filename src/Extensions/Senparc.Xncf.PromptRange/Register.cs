using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.AI.Kernel;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange
{
    [XncfRegister]
    [XncfOrder(5897)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.PromptRange";

        public override string Uid => "C6175B8E-9F79-4053-9523-F8E4AC0C3E18"; //It must be globally unique and must be fixed after generation. It has been automatically generated and can also be modified by yourself.

        public override string Version => "0.15.2"; //Version number is required

        public override string MenuName => "提示词靶场";

        public override string Icon => "fa fa-dot-circle-o";

        public override string Description => "你的提示词（Prompt）训练场";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider,
            InstallOrUpdate installOrUpdate)
        {
            //Update database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //Execute logic based on different conditions for installation or update
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //New installation

                    #region 初始化数据库数据

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
            PromptRangeSenparcEntities mySenparcEntities =
                serviceProvider.GetService(mySenparcEntitiesType) as PromptRangeSenparcEntities;

            //Specify the data entity to be deleted

            //Note: As a demonstration, all tables created by this module are deleted when uninstalling the module. During actual operation, please operate with caution and sort the entities in the order of deletion!
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion

            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion


        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
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
            services.AddScoped<PromptRangeService>();
            services.AddScoped<PromptItemService>();
            services.AddScoped<PromptResultService>();
            services.AddScoped<PromptResultChatService>();
            services.AddScoped<LlModelService>();

            services.AddAutoMapper(z =>
            {
                z.CreateMap<Domain.Models.DatabaseModel.PromptRange, PromptRangeDto>().ReverseMap();
                z.CreateMap<PromptItem, PromptItemDto>().ReverseMap();
                z.CreateMap<PromptResult, PromptResultDto>().ReverseMap();
                z.CreateMap<PromptResultChat, PromptResultChatDto>().ReverseMap();
                // z.CreateMap<LlModel, LlModelDto>().ReverseMap();
                //
                // z.CreateMap<LlModel, LlmModel_GetPageItemResponse>();

                //TODO:morek
            });

            return base.AddXncfModule(services, configuration, env);
        }
    }
}










