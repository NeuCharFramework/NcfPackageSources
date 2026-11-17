using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSessionModule))]
    [Serializable]
    public class AdminChatSessionModule : EntityBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required, MaxLength(200)]
        public string XncfModuleUid { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required, MaxLength(200)]
        public string ModuleName { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(50)]
        public string ModuleVersion { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public DateTime AddedTime { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public virtual AdminChatSession Session { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private AdminChatSessionModule() { }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
