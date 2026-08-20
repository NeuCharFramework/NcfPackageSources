using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Models.MultipleDatabase
{
    /// <summary>
    /// 用于生成达梦数据库 Migration 信息的类，请勿修改
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class XncfBuilderSenparcEntities_Dm : XncfBuilderSenparcEntities, IMultipleMigrationDbContext
    {
        public XncfBuilderSenparcEntities_Dm(DbContextOptions<XncfBuilderSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 达梦默认的 NVARCHAR2(32767) 小于预览输出上限，使用 CLOB 避免持久化中断。
            modelBuilder.Entity<XncfPreviewTask>().Property(task => task.ErrorMessage).HasColumnType("CLOB");
            modelBuilder.Entity<XncfPreviewTask>().Property(task => task.RecentOutput).HasColumnType("CLOB");
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -C XncfBuilderSenparcEntities_Dm -o Domain/Migrations/Migrations.Dm </para>
    /// </summary>
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<XncfBuilderSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //指定其他数据库
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Dm()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
