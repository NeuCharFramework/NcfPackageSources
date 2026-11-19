/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AdminChatSessionModuleDto.cs
    File description: AdminChatSessionModuleDto related implementation

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminChatSessionModuleDto : DtoBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string XncfModuleUid { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ModuleVersion { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// menu name
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ModuleDescription { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime AddedTime { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static AdminChatSessionModuleDto CreateFromEntity(AdminChatSessionModule entity)
        {
            if (entity == null) return null;

            return new AdminChatSessionModuleDto
            {
                //Legacy comment translated to English.
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                //Legacy comment translated to English.
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
