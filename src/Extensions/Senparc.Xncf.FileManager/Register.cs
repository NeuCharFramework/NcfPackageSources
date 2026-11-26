/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.1 enhance file management capabilities with new upload stream method and service registration

    Modified by: Senparc - 20261111
    Change description: v0.8.3 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

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

using Senparc.Xncf.FileManager.Models;
using Senparc.Xncf.FileManager.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.FileManager.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Senparc.CO2NET.RegisterServices;
using Senparc.Xncf.FileManager.Abstractions;
using Senparc.Xncf.FileManager.Domain.Services;

namespace Senparc.Xncf.FileManager
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<FileManagerResource>(
            "Module.FileManager",
            "File Manager",
            "Manages system files");

        #region Legacy section

        public override string Name => "Senparc.Xncf.FileManager";

        public override string Uid => "BC7769FE-E094-4EAF-9B1F-D82670D1D691";//Legacy comment translated to English.

        public override string Version => "0.7.3";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-database";

        public override string Description => RegisterText.Description;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            FileManagerSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as FileManagerSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

    /// <inheritdoc/>
    public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddScoped<ColorAppService>();
            services.AddScoped<IFileManagerGateway, NcfFileManagerGateway>();
            
            services.AddAutoMapper(z =>
            {
                z.CreateMap<Color, ColorDto>().ReverseMap();
                z.CreateMap<NcfFile, NcfFileDto>().ReverseMap();
            });
            return base.AddXncfModule(services, configuration, env);
        }

    /// <inheritdoc/>
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





