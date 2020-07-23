using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class XscfModuleDto : DtoBase
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
        public XscfModules_State State { get; private set; }
        private XscfModuleDto() { }


        public XscfModuleDto(int id, string name, string uid, string menuName, string version, string description, string updateLog, bool allowRemove, string menuId, string icon, XscfModules_State state)
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

    public class CreateOrUpdate_XscfModuleDto : DtoBase
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
        public XscfModules_State State { get; private set; }

        private CreateOrUpdate_XscfModuleDto() { }

        public CreateOrUpdate_XscfModuleDto(string name, string uid, string menuName, string version, string description, string updateLog, bool allowRemove, string menuId, string icon, XscfModules_State state)
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

    public class UpdateVersion_XscfModuleDto : DtoBase
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

        private UpdateVersion_XscfModuleDto() { }


        public UpdateVersion_XscfModuleDto(string name, string uid, string menuName, string version, string description)
        {
            Name = name;
            Uid = uid;
            MenuName = menuName;
            Version = version;
            Description = description;
        }
    }

    /// <summary>
    /// 跟新菜单Id
    /// </summary>
    public class UpdateMenuId_XscfModuleDto : DtoBase
    {
        public string Uid { get; set; }

        public string MenuId { get; private set; }

        private UpdateMenuId_XscfModuleDto() { }


        public UpdateMenuId_XscfModuleDto(string uid, string menuId)
        {
            Uid = uid;
            MenuId = menuId;
        }
    }

}
