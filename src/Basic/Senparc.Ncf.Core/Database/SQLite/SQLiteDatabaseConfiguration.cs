using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Database.SQLite
{
    public class SQLiteDatabaseConfiguration : IDatabaseConfiguration
    {
        public Action<IRelationalDbContextOptionsBuilderInfrastructure> DbContextOptionsAction => throw new NotImplementedException();

        public void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null)
        {
            throw new NotImplementedException();
        }
    }
}
