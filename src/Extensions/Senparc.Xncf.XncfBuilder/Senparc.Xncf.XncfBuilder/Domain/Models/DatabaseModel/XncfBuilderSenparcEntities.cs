/*-----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: XncfBuilderSenparcEntities.cs
    File description: XncfBuilderSenparcEntities related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.41.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder
{
    public class XncfBuilderSenparcEntities : XncfDatabaseDbContext, IMultipleMigrationDbContext
    {
        public XncfBuilderSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Config> Configs { get; set; }

        public DbSet<XncfPreviewTask> XncfPreviewTasks { get; set; }

        public DbSet<XncfPreviewHost> XncfPreviewHosts { get; set; }

        /// <summary>
        /// Persistent source of truth for isolated create/modify/validate/merge jobs.
        /// Migration is intentionally supplied by the host administrator together with the
        /// existing XncfBuilder migrations.
        /// </summary>
        public DbSet<XncfDevelopmentJob> XncfDevelopmentJobs { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point

    }
}
