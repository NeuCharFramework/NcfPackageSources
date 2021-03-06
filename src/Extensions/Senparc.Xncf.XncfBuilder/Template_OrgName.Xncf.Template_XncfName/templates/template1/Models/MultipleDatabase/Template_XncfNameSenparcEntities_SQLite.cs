
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel;

namespace Template_OrgName.Xncf.Template_XncfName.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class Template_XncfNameSenparcEntities_Sqlite : Template_XncfNameSenparcEntities
    {
        public Template_XncfNameSenparcEntities_Sqlite(DbContextOptions<Template_XncfNameSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c Template_XncfNameSenparcEntities_Sqlite -o Migrations/Migrations.Sqlite </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<Template_XncfNameSenparcEntities_Sqlite, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //指定其他数据库
            services.AddDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
