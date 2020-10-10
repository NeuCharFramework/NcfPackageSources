using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Ncf.Database.MultipleMigrationDbContext;

namespace Senparc.Ncf.Database.SqlServer
{
    public class SQLServerDatabaseConfiguration : DatabaseConfigurationBase<SqlServerDbContextOptionsBuilder, SqlServerOptionsExtension>
    {
        public SQLServerDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.SqlServer;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as SqlServerDbContextOptionsBuilder;
            typedBuilder.EnableRetryOnFailure(
                       maxRetryCount: 5,
                       maxRetryDelay: TimeSpan.FromSeconds(5),
                       errorNumbersToAdd: new int[] { 2 });
        };

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                optionsBuilder.UseSqlServer(connectionString, actionBase);//beta6
            };
    }
}
