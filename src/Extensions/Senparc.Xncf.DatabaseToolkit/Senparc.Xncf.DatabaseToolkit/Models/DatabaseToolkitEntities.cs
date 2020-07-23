using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    public class DatabaseToolkitEntities : XscfDatabaseDbContext
    {
        public override IXscfDatabase XscfDatabaseRegister => new Register();
        public DatabaseToolkitEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<DbConfig> DbConfigs { get; set; }
    }
}
