/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModule.cs
    File description: XncfModule related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// Stores persisted XNCF module metadata, menu binding, version, and lifecycle state.
    /// </summary>
    [Table("XncfModules")]
    public class XncfModule : EntityBase<int>
    {
        /// <summary>Gets the module name.</summary>
        public string Name { get; private set; }
        /// <summary>Gets the stable module UID.</summary>
        public string Uid { get; private set; }
        /// <summary>Gets the administration menu name.</summary>
        public string MenuName { get; private set; }
        /// <summary>Gets the registered module version.</summary>
        public string Version { get; private set; }
        /// <summary>Gets or sets the module description.</summary>
        public string Description { get; set; }
        /// <summary>Gets the lifecycle update log.</summary>
        public string UpdateLog { get; private set; }
        /// <summary>Gets whether removal is permitted.</summary>
        public bool AllowRemove { get; private set; }
        /// <summary>Gets the bound menu identifier.</summary>
        public string MenuId { get; private set; }
        /// <summary>Gets the module icon identifier.</summary>
        public string Icon { get; private set; }

        /// <summary>Gets the persisted module lifecycle state.</summary>
        public XncfModules_State State { get; private set; }

        /// <summary>Appends a timestamped entry to the module update log.</summary>
        /// <param name="log">The log message to append.</param>
        private void AddUpdateLog(string log)
        {
            UpdateLog += $"[{SystemTime.Now}] {log}\r\n";
        }


        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private XncfModule() { }


        /// <summary>Initializes a persisted module record.</summary>
        public XncfModule(string name, string uid, string menuName, string version, string description, string updateLog, bool allowRemove, string menuId,string icon, XncfModules_State state)
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

        /// <summary>
        /// Appends a creation entry to the module update log.
        /// </summary>
        public void Create()
        {
            AddUpdateLog($"创建新模块：{MenuName}：{Name} / {Uid}");
        }

        /// <summary>
        /// Updates module version/menu/description fields and moves the module to pending review.
        /// </summary>
        /// <param name="version">The new module version.</param>
        /// <param name="menuName">The new menu name.</param>
        /// <param name="description">The new module description.</param>
        public void UpdateVersion(string version, string menuName, string description)
        {
            AddUpdateLog($"更新模块版本号：{MenuName}。版本：{Version} > {version} 菜单：{MenuName} > {menuName}");

            Version = version;
            MenuName = menuName;
            Description = description;

            UpdateState(XncfModules_State.更新待审核);
        }

        /// <summary>
        /// Changes module state and records the transition in the update log.
        /// </summary>
        /// <param name="newState">The new module state.</param>
        public void UpdateState(XncfModules_State newState)
        {
            AddUpdateLog($"更新模块状态：{MenuName}。状态：{State} > {newState}");
            State = newState;
        }

        /// <summary>
        /// Associates the module with a menu identifier and records the binding.
        /// </summary>
        /// <param name="menuId">The menu identifier.</param>
        public void UpdateMenuId(string menuId)
        {
            MenuId = menuId;
            AddUpdateLog($"已绑定菜单");
        }
    }
}
