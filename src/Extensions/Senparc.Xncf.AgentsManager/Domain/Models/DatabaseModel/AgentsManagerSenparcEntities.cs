/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentsManagerSenparcEntities.cs
    文件功能描述：AgentsManagerSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20240616
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    public class AgentsManagerSenparcEntities : XncfDatabaseDbContext
    {
        public AgentsManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<AgentTemplate> AgentTemplates { get; set; }

        public DbSet<ChatGroup> ChatGroups { get; set; }

        public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }

        /// <summary>
        /// 通过 A2A 协议接入的远程智能体。
        /// </summary>
        public DbSet<RemoteAgent> RemoteAgents { get; set; }

        /// <summary>
        /// ChatGroup 的远程智能体成员。保留 ChatGroupMember 作为本地 Agent 的兼容表。
        /// </summary>
        public DbSet<ChatGroupRemoteMember> ChatGroupRemoteMembers { get; set; }

        /// <summary>
        /// 将本地 AgentTemplate 发布为标准 A2A 服务的附加配置。
        /// </summary>
        public DbSet<PublishedA2AAgent> PublishedA2AAgents { get; set; }

        public DbSet<ChatGroupHistory> ChatGroupHistories { get; set; }

        public DbSet<ChatTask> ChatTasks { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
