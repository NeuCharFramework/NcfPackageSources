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
        public Type DbContextOptionsBuilderType => typeof(SqlServerDbContextOptionsBuilder);

        Action<IRelationalDbContextOptionsBuilderInfrastructure> IDatabaseConfiguration.DbContextOptionsAction => b =>
        {
            if (b is SqlServerDbContextOptionsBuilder typedBuilder)
            {
                typedBuilder.EnableRetryOnFailure(
                   maxRetryCount: 5,
                   maxRetryDelay: TimeSpan.FromSeconds(5),
                   errorNumbersToAdd: new int[] { 2 });
            }
            else
            {
                throw 
            }
        };


        public void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null)
        {
            optionsBuilder.UseSqlServer(connectionString, dbContextOptionsAction);//beta6
        }
    }
}
