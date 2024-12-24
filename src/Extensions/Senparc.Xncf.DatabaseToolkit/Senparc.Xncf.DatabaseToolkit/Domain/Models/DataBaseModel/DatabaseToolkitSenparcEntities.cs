using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    public class DatabaseToolkitSenparcEntities : XncfDatabaseDbContext
    {
        public DatabaseToolkitSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<DbConfig> DbConfigs { get; set; }
    }
}
