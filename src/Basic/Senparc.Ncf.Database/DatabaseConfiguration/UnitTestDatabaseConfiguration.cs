using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// 使用 appsettigs.json 中的配置进行获取，不作为任何实际使用的数据库配置
    /// </summary>
    public class UnitTestDatabaseConfiguration : IDatabaseConfiguration
    {
        /// <summary>
        /// 单元测试环境下的默认总数据库
        /// </summary>
        public static Type UnitTestPillarDbContext { get; set; } = typeof(SenparcEntitiesBase);

        public MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.UnitTest;

        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase => throw new NotImplementedException();

        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => throw new NotImplementedException();

        public Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase => throw new NotImplementedException();

        public string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            throw new NotImplementedException();
        }

        public string GetDropTableSql(DbContext dbContext, string tableName)
        {
            throw new NotImplementedException();
        }

        public void UseDatabase(DbContextOptionsBuilder builder, string connectionString, XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            throw new NotImplementedException();
        }
    }
}
