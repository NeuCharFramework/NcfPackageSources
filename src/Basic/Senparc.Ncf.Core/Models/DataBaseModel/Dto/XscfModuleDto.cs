/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XscfModuleDto.cs
    File description: XscfModuleDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>Transfers installed XNCF module identity, version, menu, and lifecycle state.</summary>
    public class XncfModuleDto : DtoBase
    {
        /// <summary>Gets the module database identifier.</summary>
        public int Id { get; private set; }
        /// <summary>Gets the module type name.</summary>
        public string Name { get; private set; }
        /// <summary>Gets the stable module UID.</summary>
        public string Uid { get; private set; }
        /// <summary>Gets the administration menu name.</summary>
        public string MenuName { get; private set; }
        /// <summary>Gets the registered module version.</summary>
        public string Version { get; private set; }
        /// <summary>Gets the module description.</summary>
        public string Description { get; private set; }
        /// <summary>Gets the module update log.</summary>
        public string UpdateLog { get; private set; }
        /// <summary>Gets whether the module may be removed.</summary>
        public bool AllowRemove { get; private set; }
        /// <summary>Gets the bound menu identifier.</summary>
        public string MenuId { get; private set; }
        /// <summary>Gets the module icon identifier.</summary>
        public string Icon { get; private set; }
        /// <summary>Gets the persisted module lifecycle state.</summary>
        public XncfModules_State State { get; private set; }
        /// <summary>
        /// Initializes an empty DTO for mapping/materialization.
        /// </summary>
        private XncfModuleDto() { }

        /// <summary>
        /// Initializes a complete XNCF module DTO.
        /// </summary>
        /// <param name="id">The module identifier.</param>
        /// <param name="name">The module name.</param>
        /// <param name="uid">The module UID.</param>
        /// <param name="menuName">The display menu name.</param>
        /// <param name="version">The module version.</param>
        /// <param name="description">The module description.</param>
        /// <param name="updateLog">The update log.</param>
        /// <param name="allowRemove">Whether removal is allowed.</param>
        /// <param name="menuId">The bound menu identifier.</param>
        /// <param name="icon">The display icon.</param>
        /// <param name="state">The module state.</param>
        public XncfModuleDto(int id, string name, string uid, string menuName, string version, string description, string updateLog, bool allowRemove, string menuId, string icon, XncfModules_State state)
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
        }
    }

    /// <summary>Contains required fields for creating or updating an XNCF module record.</summary>
    public class CreateOrUpdate_XncfModuleDto : DtoBase
    {
        [Required, StringLength(100)]
        public string Name { get; private set; }
        [Required, StringLength(100)]
        public string Uid { get; private set; }
        [Required, StringLength(100)]
        public string MenuName { get; private set; }
        [Required]
        public string Version { get; private set; }
        [Required]
        public string Description { get; private set; }

        [Required]
        public string UpdateLog { get; private set; }
        [Required]
        public bool AllowRemove { get; private set; }
        public string MenuId { get; private set; }
        public string Icon { get; private set; }
        [Required]
        public XncfModules_State State { get; private set; }

        /// <summary>
        /// Initializes an empty create/update DTO for model binding.
        /// </summary>
        private CreateOrUpdate_XncfModuleDto() { }

        /// <summary>
        /// Initializes create/update module values.
        /// </summary>
        /// <param name="name">The module name.</param>
        /// <param name="uid">The module UID.</param>
        /// <param name="menuName">The display menu name.</param>
        /// <param name="version">The module version.</param>
        /// <param name="description">The module description.</param>
        /// <param name="updateLog">The update log.</param>
        /// <param name="allowRemove">Whether removal is allowed.</param>
        /// <param name="menuId">The bound menu identifier.</param>
        /// <param name="icon">The display icon.</param>
        /// <param name="state">The module state.</param>
        public CreateOrUpdate_XncfModuleDto(string name, string uid, string menuName, string version, string description, string updateLog, bool allowRemove, string menuId, string icon, XncfModules_State state)
        {
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
        }
    }

    /// <summary>Contains module metadata used when synchronizing a registered version.</summary>
    public class UpdateVersion_XncfModuleDto : DtoBase
    {

        [Required, StringLength(100)]
        public string Name { get; private set; }
        [Required, StringLength(100)]
        public string Uid { get; private set; }
        [Required, StringLength(100)]
        public string MenuName { get; private set; }
        [Required]
        public string Version { get; private set; }
        [Required]
        public string Description { get; private set; }
        public string Icon { get; private set; }

        /// <summary>
        /// Initializes an empty version-update DTO for model binding.
        /// </summary>
        private UpdateVersion_XncfModuleDto() { }

        /// <summary>
        /// Initializes the values required to update module version metadata.
        /// </summary>
        /// <param name="name">The module name.</param>
        /// <param name="uid">The module UID.</param>
        /// <param name="menuName">The display menu name.</param>
        /// <param name="version">The new module version.</param>
        /// <param name="description">The module description.</param>
        /// <param name="icon">The display icon.</param>
        public UpdateVersion_XncfModuleDto(string name, string uid, string menuName, string version, string description,string icon)
        {
            Name = name;
            Uid = uid;
            MenuName = menuName;
            Version = version;
            Description = description;
            Icon = icon;
        }
    }

    /// <summary>Contains a module UID and its menu binding.</summary>
    public class UpdateMenuId_XncfModuleDto : DtoBase
    {
        public string Uid { get; set; }

        public string MenuId { get; private set; }

        /// <summary>
        /// Initializes an empty menu-binding DTO for model binding.
        /// </summary>
        private UpdateMenuId_XncfModuleDto() { }

        /// <summary>
        /// Initializes a module/menu binding update.
        /// </summary>
        /// <param name="uid">The module UID.</param>
        /// <param name="menuId">The menu identifier.</param>
        public UpdateMenuId_XncfModuleDto(string uid, string menuId)
        {
            Uid = uid;
            MenuId = menuId;
        }
    }

}
