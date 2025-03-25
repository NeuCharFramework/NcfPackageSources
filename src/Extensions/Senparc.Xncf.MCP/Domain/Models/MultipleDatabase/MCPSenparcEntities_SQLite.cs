
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using System;
using System.IO;

namespace Senparc.Xncf.MCP.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class MCPSenparcEntities_Sqlite : MCPSenparcEntities
    {
        public MCPSenparcEntities_Sqlite(DbContextOptions<MCPSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c MCPSenparcEntities_Sqlite -o Domain/Migrations/Migrations.Sqlite </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<MCPSenparcEntities_Sqlite, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //指定其他数据库
            app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
