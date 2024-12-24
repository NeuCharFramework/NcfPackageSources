
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;

namespace Template_OrgName.Xncf.Template_XncfName.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
    public class Template_XncfNameSenparcEntities_SqlServer : Template_XncfNameSenparcEntities
    {
        public Template_XncfNameSenparcEntities_SqlServer(DbContextOptions<Template_XncfNameSenparcEntities_SqlServer> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c Template_XncfNameSenparcEntities_SqlServer -o Domain/Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_SqlServer : SenparcDesignTimeDbContextFactoryBase<Template_XncfNameSenparcEntities_SqlServer, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //指定其他数据库
            app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
        };

        public SenparcDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
