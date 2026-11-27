/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentsManagerSenparcEntities.cs
    File description: AgentsManagerSenparcEntities related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

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
    /// <summary>EF Core context for AgentsManager persistence.</summary>
    public class AgentsManagerSenparcEntities : XncfDatabaseDbContext
    {
        /// <summary>Initializes the AgentsManager database context.</summary>
        /// <param name="dbContextOptions">The provider-specific EF Core options.</param>
        public AgentsManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<AgentTemplate> AgentTemplates { get; set; }

        public DbSet<ChatGroup> ChatGroups { get; set; }

        public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }

        /// <summary>
        /// Gets or sets persisted remote Agent configurations.
        /// </summary>
        public DbSet<RemoteAgent> RemoteAgents { get; set; }

        /// <summary>
        /// Gets or sets remote Agent membership links.
        /// </summary>
        public DbSet<ChatGroupRemoteMember> ChatGroupRemoteMembers { get; set; }

        /// <summary>
        /// Gets or sets published A2A Agent configurations.
        /// </summary>
        public DbSet<PublishedA2AAgent> PublishedA2AAgents { get; set; }

        public DbSet<ChatGroupHistory> ChatGroupHistories { get; set; }

        public DbSet<ChatTask> ChatTasks { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
