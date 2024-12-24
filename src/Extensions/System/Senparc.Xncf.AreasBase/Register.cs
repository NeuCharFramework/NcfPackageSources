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

        public override string Version => "0.1";//必须填写版本号

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

            //提供网站根目录
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
                  //忽略循环引用
                  options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                  //options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                  //不使用驼峰样式的key
                  //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                  //设置时间格式
                  //options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
              })
              //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-2.1&tabs=aspnetcore2x
              //.AddSessionStateTempDataProvider()
              //忽略JSON序列化过程中的循环引用：https://stackoverflow.com/questions/7397207/json-net-error-self-referencing-loop-detected-for-type
              .AddRazorPagesOptions(options =>
              {
                  //自动注册  防止跨站请求伪造（XSRF/CSRF）攻击
                  options.Conventions.Add(new Senparc.Ncf.AreaBase.Conventions.AutoValidateAntiForgeryTokenModelConvention());
              });
#if DEBUG
            //Razor启用运行时编译，多个项目不需要手动编译。
            if (env.IsDevelopment())
            {
                //builder.AddRazorRuntimeCompilation(options =>
                //{
                //    //自动索引所有需要使用 RazorRuntimeCompilation 的模块
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
