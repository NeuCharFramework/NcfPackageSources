/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIAgentsHubSenparcEntities.cs
    File description: AIAgentsHubSenparcEntities related implementation

    Created by: Senparc - 20240311

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.13.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.AIAgentsHub.Models
{
    public class AIAgentsHubSenparcEntities : XncfDatabaseDbContext
    {
        public AIAgentsHubSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
