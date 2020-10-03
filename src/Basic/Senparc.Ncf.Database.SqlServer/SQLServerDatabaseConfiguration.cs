using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database.SqlServer
{
    public class SQLServerDatabaseConfiguration : DatabaseConfigurationBase<SqlServerDbContextOptionsBuilder, SqlServerOptionsExtension>
    {
        public override Action<SqlServerDbContextOptionsBuilder, XncfDatabaseData> DbContextOptionsAction => (b, xncfDatabaseData) =>
        {
            b.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: new int[] { 2 });

            base.DbContextOptionsAction(b, xncfDatabaseData);
        };

        public override void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null, XncfDatabaseData xncfDatabaseData = null)
        {
            optionsBuilder.UseSqlServer(connectionString, dbContextOptionsAction);//beta6
        }
    }
}
