using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Senparc.Xncf.ReloadPage.OHS.Hubs;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;

namespace Senparc.Xncf.ReloadPage
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.ReloadPage";

        public override string Uid => "F1415647-9AF8-9025-D21E-7943F553901C";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "自动重载页面模块";

        public override string Icon => "fa fa-file-word-o";

        public override string Description => "自动重载页面模块";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {

            return base.AddXncfModule(services, configuration);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            var hubContext = app.ApplicationServices.GetServices<IHubContext<ReloadPageHub>>();

            return base.UseXncfModule(app, registerService);
        }
    }
}
