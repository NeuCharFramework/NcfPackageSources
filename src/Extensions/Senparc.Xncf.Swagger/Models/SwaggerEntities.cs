using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Swagger.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Swagger
{
    public class SwaggerEntities : XncfDatabaseDbContext
    {
        public SwaggerEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Config> Configs { get; set; }
    }
}
