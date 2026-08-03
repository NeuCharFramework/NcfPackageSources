/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MySqlDatabaseConfiguration.cs
    文件功能描述：MySqlDatabaseConfiguration 相关实现
    
    
    创建标识：Senparc - 20201004
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.20.5-preview5 适配 MySQL 数据库安装状态检测

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;
using System.Data.Common;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Senparc.Ncf.Database.MySql
{
    /// <summary>
    /// MySQL 数据库配置
    /// </summary>
    public class MySqlDatabaseConfiguration : DatabaseConfigurationBase<MySqlDbContextOptionsBuilder, MySqlOptionsExtension>
    {
        public MySqlDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.MySql;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as MySqlDbContextOptionsBuilder;
            typedBuilder.EnableRetryOnFailure(
                       maxRetryCount: 5,
                       maxRetryDelay: TimeSpan.FromSeconds(5),
                       errorNumbersToAdd: new int[] { 2 });
        };

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                optionsBuilder.UseMySql(connectionString,
                    //ServerVersion 用法：https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/1233
                    ServerVersion.AutoDetect(connectionString), 
                    actionBase);//beta6
            };

        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            throw new NcfDatabaseException("Pomelo.EntityFrameworkCore.MySql 暂时不支持运行时备份，请使用命令行进行备份。Pomelo.EntityFrameworkCore.MySql v5.0 之后可支持。", DatabaseConfigurationFactory.Instance.Current.GetType(), null);
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            return $"DROP TABLE `{tableName}`";
        }
    }

    /// <summary>
    /// MySQL 设计时数据库配置。
    /// </summary>
    /// <remarks>
    /// 迁移生成和模型比较只需要创建 EF Core 模型，不应该要求目标数据库在线。
    /// 因此这里使用稳定的 MySQL 8.0 兼容基线，避免 <see cref="ServerVersion.AutoDetect(string)"/>
    /// 在 dotnet ef 执行期间主动建立数据库连接。正常运行时仍应使用
    /// <see cref="MySqlDatabaseConfiguration"/> 自动识别实际服务器版本。
    /// </remarks>
    public sealed class MySqlDesignTimeDatabaseConfiguration : MySqlDatabaseConfiguration
    {
        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData,
            Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
                optionsBuilder.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 0)),
                    actionBase);
    }
}
