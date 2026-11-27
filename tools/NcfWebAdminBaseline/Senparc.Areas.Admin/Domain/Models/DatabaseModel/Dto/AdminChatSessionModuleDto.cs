/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AdminChatSessionModuleDto.cs
    File description: AdminChatSessionModuleDto related implementation

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// Represents a module attached to an administrator chat session.
    /// </summary>
    public class AdminChatSessionModuleDto : DtoBase<int>
    {
        /// <summary>
        /// Gets or sets the chat session identifier.
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// Gets or sets the registered XNCF module UID.
        /// </summary>
        public string XncfModuleUid { get; set; }

        /// <summary>
        /// Gets or sets the module's internal name.
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// Gets or sets the module version recorded when it was attached.
        /// </summary>
        public string ModuleVersion { get; set; }

        /// <summary>
        /// Gets or sets the localized display name exposed to the client.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// menu name
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// Gets or sets the localized module description.
        /// </summary>
        public string ModuleDescription { get; set; }

        /// <summary>
        /// Gets or sets the timestamp at which the module was attached.
        /// </summary>
        public DateTime AddedTime { get; set; }

        /// <summary>
        /// Creates a session-module DTO from a persisted <see cref="AdminChatSessionModule"/> entity.
        /// </summary>
        /// <param name="entity">The persisted association; <see langword="null"/> returns <see langword="null"/>.</param>
        /// <returns>A populated DTO, or <see langword="null"/> when <paramref name="entity"/> is null.</returns>
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
