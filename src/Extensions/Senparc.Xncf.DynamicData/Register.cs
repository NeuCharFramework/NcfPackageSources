/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.cs

    File description: Register related implementation

    

    

    Created by: Senparc - 20240718

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260717

    Change description: v0.6.0-preview2 integrate shared resource localization and improve feature text for the DynamicData module



    Modified by: Senparc - 20261111

    Change description: v0.7.10 unify XNCF Register display metadata and localization resource keys



----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.DynamicData.Models;
using Senparc.Xncf.DynamicData.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.DynamicData.Models.DatabaseModel.Dto;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.DynamicData.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace Senparc.Xncf.DynamicData
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<DynamicDataResource>(
            "Module.DynamicData",
            "动态数据基础模块",
            "用于创建和管理动态数据（早期迭代，尚未正式发布）");

        #region Legacy section

        public override string Name => "Senparc.Xncf.DynamicData";

        public override string Uid => "9448D9BA-5DF5-4AF1-8419-1BAB85BE589E";//Legacy comment translated to English.

        public override string Version => "0.1.0";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-database";

        public override string Description => RegisterText.Description;

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
                    var colorService = serviceProvider.GetService<ColorAppService>();
                    var colorResult = await colorService.GetOrInitColorAsync();
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
            DynamicDataSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as DynamicDataSenparcEntities;

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
            services.AddScoped<ColorAppService>();
            services.AddScoped<TableMetadataService>();
            services.AddScoped<ColumnMetadataService>();
            services.AddScoped<TableDataService>();

            services.AddAutoMapper(z =>
            {
                z.CreateMap<Color, ColorDto>().ReverseMap();
                z.CreateMap<TableMetadata, TableMetadataDto>().ReverseMap();
                z.CreateMap<ColumnMetadata, ColumnMetadataDto>().ReverseMap();
                //z.CreateMap<List<ColumnMetadata>, List<ColumnMetadataDto>>().ReverseMap();
                z.CreateMap<TableData, TableDataDto>().ReverseMap();
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



