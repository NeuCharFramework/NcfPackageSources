/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SwaggerEntities.cs
    File description: SwaggerEntities related implementation

    Created by: Senparc - 20210724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
