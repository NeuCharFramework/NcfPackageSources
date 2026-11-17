/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIKernelSenparcEntities.cs
    File description: AIKernelSenparcEntities related implementation

    Created by: Senparc - 20231220

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261009
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.AIKernel.Models
{
    public class AIKernelSenparcEntities : XncfDatabaseDbContext
    {
        public AIKernelSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<AIModel> AiModels { get; set; }
        public DbSet<AIVector> AiVectors { get; set; }
        public DbSet<AITokenUsage> AiTokenUsages { get; set; }
        public DbSet<AIFineTuningWorker> AiFineTuningWorkers { get; set; }
        public DbSet<AIFineTuningDataset> AiFineTuningDatasets { get; set; }
        public DbSet<AIFineTuningJob> AiFineTuningJobs { get; set; }
        public DbSet<AIFineTuningEvent> AiFineTuningEvents { get; set; }
        public DbSet<AIFineTuningArtifact> AiFineTuningArtifacts { get; set; }
        public DbSet<AIFineTuningMigration> AiFineTuningMigrations { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        // Keep provider-specific model configuration in the generated migrations.
    }
}
