using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    ///AdminChatSessionModule: Administers background chat sessions - module association
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSessionModule))]
    [Serializable]
    public class AdminChatSessionModule : EntityBase<int>
    {
        /// <summary>
        ///Session ID (foreign key to AdminChatSession)
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        ///XNCF module unique identifier
        /// </summary>
        [Required, MaxLength(200)]
        public string XncfModuleUid { get; private set; }

        /// <summary>
        ///Module name (redundant storage for quick query)
        /// </summary>
        [Required, MaxLength(200)]
        public string ModuleName { get; private set; }

        /// <summary>
        ///Module version (redundant storage)
        /// </summary>
        [MaxLength(50)]
        public string ModuleVersion { get; private set; }

        /// <summary>
        /// time added to session
        /// </summary>
        [Required]
        public DateTime AddedTime { get; private set; }

        /// <summary>
        /// Navigation properties: associated sessions
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public virtual AdminChatSession Session { get; private set; }

        /// <summary>
        /// Private constructor (for use by EF Core)
        /// </summary>
        private AdminChatSessionModule() { }

        /// <summary>
        ///Create new session-module association
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="xncfModuleUid">XNCF module UID</param>
        /// <param name="moduleName">Module name</param>
        /// <param name="moduleVersion">Module version</param>
        public AdminChatSessionModule(int sessionId, string xncfModuleUid, string moduleName, string moduleVersion)
        {
            SessionId = sessionId;
            XncfModuleUid = xncfModuleUid ?? throw new ArgumentNullException(nameof(xncfModuleUid));
            ModuleName = moduleName ?? "未知模块";
            ModuleVersion = moduleVersion;
            AddedTime = DateTime.Now;
        }
    }
}
