/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.4.11 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

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
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<SenMapicResource>(
            "Module.SenMapic",
            "SenMapic",
            "SenMapic web crawler module");

        #region Legacy section

        public override string Name => "Senparc.Xncf.SenMapic";

        public override string Uid => "8D02B05B-7FBE-461F-9F8F-43BE0CE6EFE9";//Legacy comment translated to English.

        public override string Version => "0.1.3";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-database";

        public override string Description => RegisterText.Description;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            SenMapicSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as SenMapicSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        /// <inheritdoc />
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

        /// <inheritdoc />
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


