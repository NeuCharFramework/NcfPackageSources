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
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.AreasBase";

        public override string Uid => SiteConfig.SYSTEM_XNCF_BASE_AREAS;// "00000000-0000-0000-0001-000000000006";

        public override string Version => "0.1";//Version number is required

        public override string MenuName => "Areas 基础模块";

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => "运行 RazorPage 等页面的基础模块";

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

            //Provide website root directory
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
                  //Ignore circular references
                  options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                  //options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                  //Do not use camel case keys
                  //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                  //Set time format
                  //options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
              })
              //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-2.1&tabs=aspnetcore2x
              //.AddSessionStateTempDataProvider()
              //Ignore circular references during JSON serialization: https://stackoverflow.com/questions/7397207/json-net-error-self-referencing-loop-detected-for-type
              .AddRazorPagesOptions(options =>
              {
                  //Automatic registration to prevent cross-site request forgery (XSRF/CSRF) attacks
                  options.Conventions.Add(new Senparc.Ncf.AreaBase.Conventions.AutoValidateAntiForgeryTokenModelConvention());
              });
#if DEBUG
            //Razor enables runtime compilation, eliminating the need for manual compilation for multiple projects.
            if (env.IsDevelopment())
            {
                //builder.AddRazorRuntimeCompilation(options =>
                //{
                //    //Automatically index all modules that need to use RazorRuntimeCompilation
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
