using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    public class DatabaseToolkitEntities : XncfDatabaseDbContext
    {
        public override IXncfDatabase XncfDatabaseRegister => new Register();
        public DatabaseToolkitEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<DbConfig> DbConfigs { get; set; }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.SQLite;
    }
}
