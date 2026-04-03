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
        /// PromptItems entity in database
        /// </summary>
        public DbSet<PromptItem> PromptItems { get; set; }

        /// <summary>
        /// PromptResult entity in database
        /// </summary>
        public DbSet<PromptResult> PromptResults { get; set; }

        /// <summary>
        ///LlmModel entity in database
        /// </summary>
        public DbSet<LlModel> LlmModels { get; set; }

        public DbSet<PromptRange> PromptRanges { get; set; }

        /// <summary>
        /// PromptResultChat entity in database
        /// </summary>
        public DbSet<PromptResultChat> PromptResultChats { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //If there is no special need, the OnModelCreating method does not need to be written. Registration is already required in Register.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}