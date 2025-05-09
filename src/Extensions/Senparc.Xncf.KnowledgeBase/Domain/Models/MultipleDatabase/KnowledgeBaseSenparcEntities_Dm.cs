
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.KnowledgeBase.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class KnowledgeBaseSenparcEntities_Dm : KnowledgeBaseSenparcEntities
    {
        public KnowledgeBaseSenparcEntities_Dm(DbContextOptions<KnowledgeBaseSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c KnowledgeBaseSenparcEntities_Dm -o Domain/Migrations/Migrations.Dm </para>
    /// </summary>
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<KnowledgeBaseSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //指定其他数据库
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
