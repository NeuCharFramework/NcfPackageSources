/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：Register 相关实现
    
    
    创建标识：Senparc - 20211128
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.15.0-preview3 为 SystemManager 模块接入统一资源本地化并优化功能文案

    修改标识：Senparc - 20260803
    修改描述：v1.1.3 增加 FooterContent 数据库迁移

    修改标识：Senparc - 20260804
    修改描述：v0.15.3-preview6 补齐 SystemConfig.FooterContent 数据库迁移

    修改标识：Senparc - 20261111
    修改描述：v0.15.12 统一 XNCF Register 显示元数据与多语言资源键

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
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.SystemCore.Domain.Database;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.SystemManager
{
    [XncfRegister]
    [XncfOrder(5970)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<SystemManagerResource>(
            "Module.SystemManager",
            "系统管理",
            "负责系统配置和核心运行数据的管理模块，请勿删除；操作前请备份数据。");

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.SystemManager";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SERVICE_MANAGER_UID;// "00000000-0000-0000-0000-000000000002";

        public override string Version => "1.1.3";//必须填写版本号

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => RegisterText.Description;

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            ////SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            //BasePoolEntities basePoolEntities = serviceProvider.GetService<BasePoolEntities>();

            //var senparcEntities = serviceProvider.GetService(this.TryGetXncfDatabaseDbContextType) as TenantSenparcEntities;
            //await XncfDatabaseDbContext.MigrateOnInstallAsync(senparcEntities);

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //TODO：应该提供一个 BeforeUninstall 方法，阻止卸载。

            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            return base.AddXncfModule(services, configuration, env);
        }
    }
}



