using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// </summary>
    public class SenparcDbContextFactory : SenparcDesignTimeDbContextFactoryBase<DatabaseToolkitEntities, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //services.AddDatabase<SQLServerDatabaseConfiguration>();//指定其他数据库
            services.AddDatabase("Senparc.Ncf.Database", "Senparc.Ncf.Database.SQLite", "SQLiteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory()
            : base(
                 /* Debug模式下项目根目录
                 /* 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息 */
                 Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))
        {
        }
    }
}
