/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRangeSenparcEntities.cs
    File description: PromptRangeSenparcEntities related implementation

    Created by: Senparc - 20231001

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    public class PromptRangeSenparcEntities : XncfDatabaseDbContext
    {
        public PromptRangeSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        /// <summary>Gets or sets persisted prompt items.</summary>
        public DbSet<PromptItem> PromptItems { get; set; }

        /// <summary>Gets or sets persisted prompt results.</summary>
        public DbSet<PromptResult> PromptResults { get; set; }

        /// <summary>Gets or sets persisted language-model configurations.</summary>
        public DbSet<LlModel> LlmModels { get; set; }

        public DbSet<PromptRange> PromptRanges { get; set; }

        /// <summary>Gets or sets persisted prompt-result chat messages.</summary>
        public DbSet<PromptResultChat> PromptResultChats { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
