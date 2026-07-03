/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：Register 相关实现
    
    
    创建标识：Senparc - 20211124
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.SMS;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.SmsExtension
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.SmsExtension";

        public override string Uid => "7917897C-796E-B02A-B329-5CA9007A9217";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "短信 插件";

        public override string Icon => "fa fa-file-word-o";

        public override string Description => "短信 插件";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.Configure<SenparcSmsSetting>(configuration.GetSection("SenparcSmsSetting"));

            return base.AddXncfModule(services, configuration, env);
        }
    }
}
