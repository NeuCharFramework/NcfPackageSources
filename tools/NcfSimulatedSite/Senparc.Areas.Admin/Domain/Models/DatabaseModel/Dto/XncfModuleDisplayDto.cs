/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: XncfModuleDisplayDto.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
namespace Senparc.Areas.Admin.Domain
{
    /// <summary>
    ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public bool HasNewVersion { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string NewVersion { get; set; }
        public object Menus { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public object Functions { get; set; }

        private XncfModuleDisplayDto() { }

        /// <summary>
        /// Replaces persisted display text with the current registered module
        /// metadata without changing the database record.
        /// </summary>
        public void ApplyLocalizedMetadata(string menuName, string description)
        {
            MenuName = menuName;
            Description = description;
        }


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
