/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRangeSenparcEntities.cs
    File description: PromptRangeSenparcEntities related implementation

    Created by: Senparc - 20231001

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<PromptItem> PromptItems { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<PromptResult> PromptResults { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<LlModel> LlmModels { get; set; }

        public DbSet<PromptRange> PromptRanges { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<PromptResultChat> PromptResultChats { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}