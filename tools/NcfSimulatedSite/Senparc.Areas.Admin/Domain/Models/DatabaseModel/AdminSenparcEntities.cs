using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Database;
using System;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// Current Entities only exist to help SenparcEntities generate Migration information and have no special operational significance.
    /// </summary>
    public class AdminSenparcEntities : XncfDatabaseDbContext
    {
        public AdminSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        #region 系统表（无特殊情况不要修改）

        /// <summary>
        ///system settings
        /// </summary>
        public DbSet<AdminUserInfo> SystemConfigs { get; set; }

        /// <summary>
        ///Manage background chat sessions
        /// </summary>
        public DbSet<AdminChatSession> AdminChatSessions { get; set; }

        /// <summary>
        ///Manage background chat messages
        /// </summary>
        public DbSet<AdminChatMessage> AdminChatMessages { get; set; }

        /// <summary>
        ///Manage background chat session-module association
        /// </summary>
        public DbSet<AdminChatSessionModule> AdminChatSessionModules { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point

        #endregion
    }
}
