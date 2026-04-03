using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.SenMapic.Models;
using Senparc.Xncf.SenMapic.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.SenMapic.Models.DatabaseModel.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Senparc.CO2NET.RegisterServices;
using Senparc.Xncf.SenMapic.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.SenMapic.Domain.Services;

namespace Senparc.Xncf.SenMapic
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.SenMapic";

        public override string Uid => "8D02B05B-7FBE-461F-9F8F-43BE0CE6EFE9";//It must be globally unique and must be fixed after generation. It has been automatically generated and can also be modified by yourself.

        public override string Version => "0.1.3";//Version number is required

        public override string MenuName => "SenMapic";

        public override string Icon => "fa fa-database";

        public override string Description => "SenMapic 爬虫模块";

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
                    var colorService = serviceProvider.GetService<ColorAppService>();
                    var colorResult = await colorService.GetOrInitColorAsync();
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
            SenMapicSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as SenMapicSenparcEntities;

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
            services.AddScoped<ColorAppService>();
            services.AddScoped<SenMapicTaskService>();
            services.AddScoped<SenMapicTaskItemService>();

            services.AddAutoMapper(z =>
            {
                z.CreateMap<Color, ColorDto>().ReverseMap();
                z.CreateMap<SenMapicTask, SenMapicTaskDto>().ReverseMap();
                z.CreateMap<SenMapicTask, SenMapicTask_CreateUpdateDto>().ReverseMap();
                z.CreateMap<SenMapicTaskItem, SenMapicTaskItem_ListItemDto>().ReverseMap();
            });
            
            
            
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
}


