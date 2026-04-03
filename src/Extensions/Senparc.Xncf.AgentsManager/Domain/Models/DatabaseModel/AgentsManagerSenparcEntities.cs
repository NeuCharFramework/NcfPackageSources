using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.AgentsManager.Models
{
    public class AgentsManagerSenparcEntities : XncfDatabaseDbContext
    {
        public AgentsManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<AgentTemplate> AgentTemplates { get; set; }

        public DbSet<ChatGroup> ChatGroups { get; set; }

        public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }

        public DbSet<ChatGroupHistory> ChatGroupHistories { get; set; }

        public DbSet<ChatTask> ChatTasks { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //If there is no special need, the OnModelCreating method does not need to be written. Registration is already required in Register.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
