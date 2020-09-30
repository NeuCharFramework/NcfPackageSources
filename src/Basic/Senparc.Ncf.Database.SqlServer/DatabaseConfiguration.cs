using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Senparc.Ncf.Core.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database.SqlServer
{
    public class DatabaseConfiguration : IDatabaseConfiguration
    {
        public Action<SqlServerDbContextOptionsBuilder> DbContextOptionsAction => b =>
        {
            b.EnableRetryOnFailure(
                   maxRetryCount: 5,
                   maxRetryDelay: TimeSpan.FromSeconds(5),
                   errorNumbersToAdd: new int[] { 2 });
        };

        Action<IRelationalDbContextOptionsBuilderInfrastructure> IDatabaseConfiguration.DbContextOptionsAction => throw new NotImplementedException();

        public void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null)
        {
            optionsBuilder.UseSqlServer(connectionString, sqlServerOptionsAction);//beta6
        }

        public void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null)
        {
            throw new NotImplementedException();
        }
    }
}
