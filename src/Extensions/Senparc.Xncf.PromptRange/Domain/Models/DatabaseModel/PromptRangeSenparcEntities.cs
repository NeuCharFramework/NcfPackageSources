
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.PromptRange.Models
{
    public class PromptRangeSenparcEntities : XncfDatabaseDbContext
    {
        public PromptRangeSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        //public DbSet<Color> Colors { get; set; }

        /// <summary>
        /// 数据库中的 PromptGrpups 实体
        /// </summary>
        //public DbSet<PromptGroup> PromptGroups { get; set; }

        /// <summary>
        /// 数据库中的 PromptItems 实体
        /// </summary>
        public DbSet<PromptItem> PromptItems { get; set; }

        /// <summary>
        /// 数据库中的 LlmModel 实体
        /// </summary>
        //public DbSet<LlmModel> LlmModels { get; set; }

        /// <summary>
        /// 数据库中的 PromptResult 实体
        /// </summary>
        public DbSet<PromptResult> PromptResults { get; set; }


        public DbSet<LlmModel> LlmModels { get; private set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}