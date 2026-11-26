/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModuleDisplayDto.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
namespace Senparc.Areas.Admin.Domain
{
    /// <summary>
    /// Carries the persisted and runtime metadata displayed for an XNCF module in administration.
    /// </summary>
    public class XncfModuleDisplayDto : DtoBase
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Uid { get; private set; }
        public string MenuName { get; private set; }
        public string Version { get; private set; }
        public string Description { get; private set; }
        public string UpdateLog { get; private set; }
        public bool AllowRemove { get; private set; }
        public string MenuId { get; private set; }
        public string Icon { get; private set; }
        public XncfModules_State State { get; private set; }

        /// <summary>
        /// Gets or sets whether a newer registered version is available.
        /// </summary>
        public bool HasNewVersion { get; set; }

        /// <summary>
        /// Gets or sets the display name of the available newer version.
        /// </summary>
        public string NewVersion { get; set; }
        /// <summary>
        /// Gets or sets the area menu items supplied by the registered module.
        /// </summary>
        public object Menus { get; set; }

        /// <summary>
        /// Gets or sets the function descriptors exposed by the registered module.
        /// </summary>
        public object Functions { get; set; }

        private XncfModuleDisplayDto() { }

        /// <summary>
        /// Replaces persisted display text with the current registered module
        /// metadata without changing the database record.
        /// </summary>
        /// <summary>
        /// Replaces the persisted menu name and description with current registered metadata
        /// without changing the underlying module entity.
        /// </summary>
        /// <param name="menuName">The localized menu name supplied by the registered module.</param>
        /// <param name="description">The localized description supplied by the registered module.</param>
        public void ApplyLocalizedMetadata(string menuName, string description)
        {
            MenuName = menuName;
            Description = description;
        }


        /// <summary>
        /// Initializes a display DTO with persisted module metadata and optional runtime details.
        /// </summary>
        /// <param name="id">The persisted module identifier.</param>
        /// <param name="name">The internal module name.</param>
        /// <param name="uid">The stable module UID.</param>
        /// <param name="menuName">The administration menu name.</param>
        /// <param name="version">The installed version.</param>
        /// <param name="description">The module description.</param>
        /// <param name="updateLog">The module update log.</param>
        /// <param name="allowRemove">Whether the module may be removed.</param>
        /// <param name="menuId">The persisted menu identifier.</param>
        /// <param name="icon">The module icon descriptor.</param>
        /// <param name="state">The persisted module state.</param>
        /// <param name="hasNewVersion">Whether a newer version is available.</param>
        /// <param name="newVersion">The display name of the newer version.</param>
        /// <param name="menus">The runtime area menu items.</param>
        /// <param name="functions">The runtime function descriptors, when available.</param>
        public XncfModuleDisplayDto(int id, string name, string uid, string menuName, string version, string description, string updateLog, bool allowRemove, string menuId, string icon, XncfModules_State state, bool hasNewVersion, string newVersion, object menus, object functions = null)
        {
            Id = id;
            Name = name;
            Uid = uid;
            MenuName = menuName;
            Version = version;
            Description = description;
            UpdateLog = updateLog;
            AllowRemove = allowRemove;
            MenuId = menuId;
            Icon = icon;
            State = state;
            HasNewVersion = hasNewVersion;
            NewVersion = newVersion;
            Menus = menus;
            Functions = functions;
        }

    }
}
