using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.XncfBuilder
{
    public class ConfigDto : DtoBase
    {
        /// <summary>
        /// Sln 文件路径
        /// </summary>
        [MaxLength(300)]
        public string SlnPath { get; set; }
        /// <summary>
        /// 组织名称
        /// </summary>
        [MaxLength(300)]
        public string OrgName { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        [MaxLength(50)]
        public string ModuleName { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        [MaxLength(100)]
        public string Version { get; set; }

        /// <summary>
        /// 菜单名称
        /// </summary>
        [MaxLength(100)]
        public string MenuName { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [MaxLength(100)]
        public string Icon { get; set; }

        private ConfigDto() { }

    }
}
