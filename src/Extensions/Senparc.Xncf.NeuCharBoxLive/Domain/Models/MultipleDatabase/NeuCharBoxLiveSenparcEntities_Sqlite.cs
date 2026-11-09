/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharBoxLiveSenparcEntities_Sqlite.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：NeuCharBoxLiveSenparcEntities_Sqlite


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class NeuCharBoxLiveSenparcEntities_Sqlite : NeuCharBoxLiveSenparcEntities
    {
        public NeuCharBoxLiveSenparcEntities_Sqlite(DbContextOptions<NeuCharBoxLiveSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }

        /// <summary>Every SQLite connection this context opens gets <c>PRAGMA secure_delete = ON</c>, so text the retention timer blanks does not linger in freed pages.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddInterceptors(SecureDeleteInterceptor.Instance);
        }
    }

    public sealed class SecureDeleteInterceptor : DbConnectionInterceptor
    {
        public static readonly SecureDeleteInterceptor Instance = new();

        const string Sql = "PRAGMA secure_delete = ON;";

        public override void ConnectionOpened(System.Data.Common.DbConnection connection, ConnectionEndEventData eventData)
        {
            Apply(connection);
            base.ConnectionOpened(connection, eventData);
        }

        public override async Task ConnectionOpenedAsync(System.Data.Common.DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = Sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }

        static void Apply(System.Data.Common.DbConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = Sql;
            cmd.ExecuteNonQuery();
        }
    }


    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：dotnet ef migrations add [更新名称] -p src/Senparc.Xncf.NeuCharBoxLive -c NeuCharBoxLiveSenparcEntities_Sqlite -o Domain/Migrations/Sqlite（在仓库根目录运行） </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<NeuCharBoxLiveSenparcEntities_Sqlite, Register>
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
