using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xncf.DatabaseToolkit
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        public Register()
        { }

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.DatabaseToolkit";

        public override string Uid => "3019CCBE-0739-43D5-9DED-027A0B26745E";//必须确保全局唯一，生成后必须固定
        public override string Version => "0.7.0";//必须填写版本号

        public override string MenuName => "数据库工具包";
        public override string Icon => "fa fa-database";
        public override string Description => "为方便数据库操作提供的工具包。请完全了解本工具各项功能特点后再使用，所有数据库操作都有损坏数据的可能，修改数据库前务必注意数据备份！";

        /// <summary>
        /// 注册当前模块需要支持的功能模块
        /// </summary>
        public override IList<Type> Functions => new[] {
            typeof(Functions.SetConfig),
            typeof(Functions.BackupDatabase),
            typeof(Functions.ShowDatabaseConfiguration),
            typeof(Functions.ExportSQL),
            typeof(Functions.CheckUpdate),
            typeof(Functions.UpdateDatabase),
        };

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await base.MigrateDatabaseAsync(serviceProvider);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            DatabaseToolkitEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as DatabaseToolkitEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion

            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion
    }
}