using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.BaseAreas
{
    [XncfRegister]
    [XncfOrder(5955)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.BaseAreas";

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

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {

#if DEBUG
            //Razor启用运行时编译，多个项目不需要手动编译。
            if (env.IsDevelopment())
            {
                builder.AddRazorRuntimeCompilation(options =>
                {
                    //自动索引所有需要使用 RazorRuntimeCompilation 的模块
                    foreach (var razorRegister in XncfRegisterManager.RegisterList.Where(z => z is IXncfRazorRuntimeCompilation))
                    {
                        try
                        {
                            var libraryPath = ((IXncfRazorRuntimeCompilation)razorRegister).LibraryPath;
                            options.FileProviders.Add(new PhysicalFileProvider(libraryPath));
                        }
                        catch (Exception ex)
                        {
                            SenparcTrace.BaseExceptionLog(ex);
                        }
                    }
                });
            }
#endif
            return base.AddXncfModule(services, configuration);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            return base.UseXncfModule(app, registerService);
        }
    }
}
