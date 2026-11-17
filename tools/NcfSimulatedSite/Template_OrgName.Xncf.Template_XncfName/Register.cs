/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Register.cs
    File description: XNCF template sample implementation
    
    
    Created by: Senparc - 20200818
    
    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v1.1.13 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v1.1.14 unify XNCF Register display metadata and localization resource keys

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

#if XNCF_HAS_DATABASE
using Template_OrgName.Xncf.Template_XncfName.Models;
using Template_OrgName.Xncf.Template_XncfName.Application.AppServices;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;
#endif
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Template_OrgName.Xncf.Template_XncfName
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText =
            XncfRegisterText.Create<Template_XncfNameResource>(
                "Module",
                "Template_MenuName",
                "Template_Description");

        #region Legacy section

        public override string Name => "Template_OrgName.Xncf.Template_XncfName";

        public override string Uid => "C5852057-BF1D-492B-A22E-64F3C5F91D38";//Legacy comment translated to English.

        public override string Version => "1.0.0";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "Template_Icon";

        public override string Description => RegisterText.Description;

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
#if XNCF_HAS_DATABASE
            //update the database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
#endif
            //execute logic according to the installation or update condition
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //new installation
#if XNCF_HAS_DATABASE
            #region Legacy section
                    var colorService = serviceProvider.GetService<ColorAppService>();
                    var colorResult = await colorService.GetOrInitColorAsync();
            #endregion
#endif
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
#if XNCF_HAS_DATABASE
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            Template_XncfNameSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as Template_XncfNameSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
#endif
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
#if XNCF_HAS_DATABASE
            services.AddScoped<ColorAppService>();
            services.AddScoped<ColorService>();

            services.AddAutoMapper(z =>
            {
                z.CreateMap<Color, ColorDto>().ReverseMap();
            });
#endif
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
