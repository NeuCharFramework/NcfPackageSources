/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：Register 相关实现
    
    
    创建标识：Senparc - 20211128
    
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
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.Menu
{
    [XncfRegister]
    [XncfOrder(5940)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.Menu";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_MENU_UID;// "00000000-0000-0000-0000-000000000005";

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "菜单管理";

        public override string Icon => "fa fa-bars";

        public override string Description => "系统菜单管理";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            return base.AddXncfModule(services, configuration, env);
        }
    }
}
