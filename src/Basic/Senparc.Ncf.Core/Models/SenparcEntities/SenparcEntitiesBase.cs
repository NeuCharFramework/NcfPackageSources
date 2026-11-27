/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcEntitiesBase.cs
    File description: SenparcEntitiesBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    public abstract class SenparcEntitiesBase : SenparcEntitiesDbContextBase, ISenparcEntitiesDbContext
    {
        /// <summary>
        /// Initializes the legacy-compatible NCF DbContext base with EF Core options
        /// and the service provider used for tenant/database services.
        /// </summary>
        /// <param name="options">The EF Core context options.</param>
        /// <param name="serviceProvider">The dependency provider for the context.</param>
        protected SenparcEntitiesBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Console.WriteLine("\t SenparcEntitiesBase OnModelCreating");

            #region Legacy section
            #endregion

            base.OnModelCreating(modelBuilder);
        }
    }
}
