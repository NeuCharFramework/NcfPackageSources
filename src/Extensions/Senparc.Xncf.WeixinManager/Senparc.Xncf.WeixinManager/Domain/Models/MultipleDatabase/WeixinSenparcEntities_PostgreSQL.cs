using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.WeixinManager.Domain.Models.MultipleDatabase
{
    /// <summary>
    /// 用于生成 PostgreSQL 数据库 Migration 信息的类，请勿修改
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
    public class WeixinSenparcEntities_PostgreSQL : WeixinSenparcEntities, IMultipleMigrationDbContext
    {
        public WeixinSenparcEntities_PostgreSQL(DbContextOptions<WeixinSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -C WeixinSenparcEntities_PostgreSQL -o Migrations/Migrations.PostgreSQL </para>
    /// </summary>
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<WeixinSenparcEntities_PostgreSQL, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //指定其他数据库
            app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL()
            : base(
                 /* Debug模式下项目根目录
                 /* 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息 */
                 Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))
        {

        }
    }
}
