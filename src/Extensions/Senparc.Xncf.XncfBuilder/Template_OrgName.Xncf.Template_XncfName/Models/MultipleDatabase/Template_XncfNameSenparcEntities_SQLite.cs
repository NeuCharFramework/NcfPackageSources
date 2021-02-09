
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;

namespace Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel
{
    [MultipleMigrationDbContext(MultipleDatabaseType.SQLite, typeof(Register))]
    public class Template_XncfNameSenparcEntities_SQLite : Template_XncfNameSenparcEntities
    {
        public Template_XncfNameSenparcEntities_SQLite(DbContextOptions<Template_XncfNameSenparcEntities_SQLite> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c Template_XncfNameSenparcEntities_SQLite -o Migrations/Migrations.SQLite </para>
    /// </summary>
    public class SenparcDbContextFactory_SQLite : SenparcDesignTimeDbContextFactoryBase<Template_XncfNameSenparcEntities_SQLite, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //指定其他数据库
            services.AddDatabase("Senparc.Ncf.Database.SQLite", "Senparc.Ncf.Database.SQLite", "SQLiteDatabaseConfiguration");
        };

        public SenparcDbContextFactory_SQLite() : base(SenparcDbContextFactory.RootDictionaryPath)
        {

        }
    }
}
