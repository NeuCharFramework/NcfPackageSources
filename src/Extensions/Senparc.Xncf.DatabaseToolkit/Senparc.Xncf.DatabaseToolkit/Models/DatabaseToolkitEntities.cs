using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    [MultipleMigrationDbContext(MultipleDatabaseType.SQLite, typeof(Register))]
    public class DatabaseToolkitEntities : XncfDatabaseDbContext
    {
        public DatabaseToolkitEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<DbConfig> DbConfigs { get; set; }
    }
}
