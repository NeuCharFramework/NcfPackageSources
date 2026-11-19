/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.13.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.13.12 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.13.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Senparc.Xncf.AreasBase
{
    [XncfRegister]
    [XncfOrder(5955)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<AreasBaseResource>(
            "Module.AreasBase",
            "Areas 基础模块",
            "为 Razor Pages 等页面提供基础运行能力");

        #region Legacy section

        public override string Name => "Senparc.Xncf.AreasBase";

        public override string Uid => SiteConfig.SYSTEM_XNCF_BASE_AREAS;// "00000000-0000-0000-0001-000000000006";

        public override string Version => "0.1";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => RegisterText.Description;

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {

            //Legacy comment translated to English.
            if (env.ContentRootPath != null)
            {
                SiteConfig.ApplicationPath = env.ContentRootPath;
                if (env is IWebHostEnvironment webEnv)
                {
                    SiteConfig.WebRootPath = webEnv.WebRootPath;
                }
            }

            var builder = services.AddRazorPages(opt =>
            {
                //opt.RootDirectory = "/";
            })
              .AddXmlSerializerFormatters()
              .AddJsonOptions(options =>
              {
                  //Legacy comment translated to English.
                  options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                  //options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                  //Legacy comment translated to English.
                  //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                  //Legacy comment translated to English.
                  //options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
              })
              //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-2.1&tabs=aspnetcore2x
              //.AddSessionStateTempDataProvider()
              //Legacy comment translated to English.
              .AddRazorPagesOptions(options =>
              {
                  //Legacy comment translated to English.
                  options.Conventions.Add(new Senparc.Ncf.AreaBase.Conventions.AutoValidateAntiForgeryTokenModelConvention());
              });
#if DEBUG
            //Legacy comment translated to English.
            if (env.IsDevelopment())
            {
                //builder.AddRazorRuntimeCompilation(options =>
                //{
                //Legacy comment translated to English.
                //    foreach (var razorRegister in XncfRegisterManager.RegisterList.Where(z => z is IXncfRazorRuntimeCompilation))
                //    {
                //        try
                //        {
                //            var libraryPath = ((IXncfRazorRuntimeCompilation)razorRegister).LibraryPath;
                //            options.FileProviders.Add(new PhysicalFileProvider(libraryPath));
                //        }
                //        catch (Exception ex)
                //        {
                //            SenparcTrace.BaseExceptionLog(ex);
                //        }
                //    }
                //});
            }
#endif
            return base.AddXncfModule(services, configuration, env);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
            return base.UseXncfModule(app, registerService);
        }

    }
}
