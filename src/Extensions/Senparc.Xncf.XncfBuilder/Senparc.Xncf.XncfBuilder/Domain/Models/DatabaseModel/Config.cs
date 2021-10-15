using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text;

namespace Senparc.Xncf.XncfBuilder
{
    [Table(Register.DATABASE_PREFIX + nameof(Config))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class Config : EntityBase<int>
    {
        /// <summary>
        /// Sln 文件路径
        /// </summary>
        [MaxLength(300)]
        public string SlnFilePath { get; private set; }
        /// <summary>
        /// 组织名称
        /// </summary>
        [MaxLength(300)]
        public string OrgName { get; private set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        [MaxLength(50)]
        public string XncfName { get; private set; }

        /// <summary>
        /// 版本号
        /// </summary>
        [MaxLength(100)]
        public string Version { get; private set; }

        /// <summary>
        /// 菜单名称
        /// </summary>
        [MaxLength(100)]
        public string MenuName { get; private set; }

        /// <summary>
        /// 图标
        /// </summary>
        [MaxLength(100)]
        public string Icon { get; private set; }

        private Config() { }

        public Config(string slnFilePath, string orgName, string xncfName, string version, string menuName, string icon)
        {
            SlnFilePath = slnFilePath;
            OrgName = orgName;
            XncfName = xncfName;
            Version = version;
            MenuName = menuName;
            Icon = icon;
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
