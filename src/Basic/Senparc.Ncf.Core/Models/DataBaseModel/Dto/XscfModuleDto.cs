using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class XncfModuleDto : DtoBase
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
        private XncfModuleDto() { }


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

        private CreateOrUpdate_XncfModuleDto() { }

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

        private UpdateVersion_XncfModuleDto() { }


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

    /// <summary>
    /// 跟新菜单Id
    /// </summary>
    public class UpdateMenuId_XncfModuleDto : DtoBase
    {
        public string Uid { get; set; }

        public string MenuId { get; private set; }

        private UpdateMenuId_XncfModuleDto() { }


        public UpdateMenuId_XncfModuleDto(string uid, string menuId)
        {
            Uid = uid;
            MenuId = menuId;
        }
    }

}
