using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    ///AdminChatSessionModuleDto: Administers background chat session - module associated data transfer object
    /// </summary>
    public class AdminChatSessionModuleDto : DtoBase<int>
    {
        /// <summary>
        /// session id
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        ///XNCF module unique identifier
        /// </summary>
        public string XncfModuleUid { get; set; }

        /// <summary>
        /// module name
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        ///module version
        /// </summary>
        public string ModuleVersion { get; set; }

        /// <summary>
        /// Module name used for front-end display (priority menu name)
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///menu name
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        ///Module brief description
        /// </summary>
        public string ModuleDescription { get; set; }

        /// <summary>
        /// time added to session
        /// </summary>
        public DateTime AddedTime { get; set; }

        /// <summary>
        /// Mapping from entities to DTOs
        /// </summary>
        public static AdminChatSessionModuleDto CreateFromEntity(AdminChatSessionModule entity)
        {
            if (entity == null) return null;

            return new AdminChatSessionModuleDto
            {
                // Explicitly copy base class properties
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                // Copy business attributes
                SessionId = entity.SessionId,
                XncfModuleUid = entity.XncfModuleUid,
                ModuleName = entity.ModuleName,
                ModuleVersion = entity.ModuleVersion,
                DisplayName = entity.ModuleName,
                AddedTime = entity.AddedTime
            };
        }
    }
}
