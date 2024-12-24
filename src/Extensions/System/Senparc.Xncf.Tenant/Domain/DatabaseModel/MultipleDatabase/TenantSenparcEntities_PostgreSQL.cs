
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.Tanent.Domain.DatabaseModel
{
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Senparc.Xncf.Tenant.Register))]
    public class TenantSenparcEntities_PostgreSQL : TenantSenparcEntities
    {
        public TenantSenparcEntities_PostgreSQL(DbContextOptions<TenantSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c TenantSenparcEntities_PostgreSQL -o Migrations/Migrations.PostgreSQL </para>
    /// </summary>
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<TenantSenparcEntities_PostgreSQL, Senparc.Xncf.Tenant.Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //指定其他数据库
            app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
