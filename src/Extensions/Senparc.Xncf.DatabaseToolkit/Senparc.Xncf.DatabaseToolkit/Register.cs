using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using static Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService.DatabaseConfigAppService;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.DatabaseToolkit
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        public Register()
        { }

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.DatabaseToolkit";

        public override string Uid => "3019CCBE-0739-43D5-9DED-027A0B26745E";//Must ensure global uniqueness and must be fixed after generation
        public override string Version => "0.7.1";//Version number is required

        public override string MenuName => "数据库工具包";
        public override string Icon => "fa fa-database";
        public override string Description => "为方便数据库操作提供的工具包。请完全了解本工具各项功能特点后再使用，所有数据库操作都有损坏数据的可能，修改数据库前务必注意数据备份！";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //Update database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            DatabaseToolkitSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as DatabaseToolkitSenparcEntities;

            //Specify the data entity to be deleted

            //Note: As a demonstration, all tables created by this module are deleted when uninstalling the module. During actual operation, please operate with caution and sort the entities in the order of deletion!
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion

            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion
    }
}