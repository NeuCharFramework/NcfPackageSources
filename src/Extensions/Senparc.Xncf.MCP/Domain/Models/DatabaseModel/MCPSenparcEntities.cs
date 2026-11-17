/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: MCPSenparcEntities.cs

    File description: MCPSenparcEntities related implementation

    

    

    Created by: Senparc - 20250325

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20261103

    Change description: v0.5.10 Remove DatabaseSample and related components from MCP extension



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.MCP.Models.DatabaseModel;

namespace Senparc.Xncf.MCP.Models
{
    public class MCPSenparcEntities : XncfDatabaseDbContext
    {
        public MCPSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<MCPEndpoint> MCPEndpoints { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
