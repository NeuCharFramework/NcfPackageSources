using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// 扩展模块信息
    /// </summary>
    public class XncfModule : EntityBase<int>
    {
        public string Name { get; private set; }
        public string Uid { get; private set; }
        public string MenuName { get; private set; }
        public string Version { get; private set; }
        public string Description { get; set; }
        public string UpdateLog { get; private set; }
        public bool AllowRemove { get; private set; }
        public string MenuId { get; private set; }
        /// <summary>
        /// 使用 FontAwesome 图标，如：fa fa-star
        /// </summary>
        public string Icon { get; private set; }

        public XncfModules_State State { get; private set; }

        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="log"></param>
        private void AddUpdateLog(string log)
        {
            UpdateLog += $"[{SystemTime.Now}] {log}\r\n";
        }


        private XncfModule() { }


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

        public void Create()
        {
            AddUpdateLog($"创建新模块：{MenuName}：{Name} / {Uid}");
        }

        public void UpdateVersion(string version, string menuName, string description)
        {
            AddUpdateLog($"更新模块版本号：{MenuName}。版本：{Version} > {version} 菜单：{MenuName} > {menuName}");

            Version = version;
            MenuName = menuName;
            Description = description;

            UpdateState(XncfModules_State.更新待审核);
        }

        public void UpdateState(XncfModules_State newState)
        {
            AddUpdateLog($"更新模块状态：{MenuName}。状态：{State} > {newState}");
            State = newState;
        }

        public void UpdateMenuId(string menuId)
        {
            MenuId = menuId;
            AddUpdateLog($"已绑定菜单");
        }
    }
}
