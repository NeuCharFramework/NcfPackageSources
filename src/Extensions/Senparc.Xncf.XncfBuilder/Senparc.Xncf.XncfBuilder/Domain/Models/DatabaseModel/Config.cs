using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text;

namespace Senparc.Xncf.XncfBuilder
{
    [Table(Register.DATABASE_PREFIX + nameof(Config))]//The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class Config : EntityBase<int>
    {
        /// <summary>
        ///Sln file path
        /// </summary>
        [MaxLength(300)]
        public string SlnFilePath { get; private set; }
        /// <summary>
        ///organization name
        /// </summary>
        [MaxLength(300)]
        public string OrgName { get; private set; }

        /// <summary>
        /// module name
        /// </summary>
        [MaxLength(50)]
        public string XncfName { get; private set; }

        /// <summary>
        /// version number
        /// </summary>
        [MaxLength(100)]
        public string Version { get; private set; }

        /// <summary>
        ///menu name
        /// </summary>
        [MaxLength(100)]
        public string MenuName { get; private set; }

        /// <summary>
        /// icon
        /// </summary>
        [MaxLength(100)]
        public string Icon { get; private set; }

        /// <summary>
        /// illustrate
        /// </summary>
        [MaxLength(400)]
        public string Description { get; private set; }

        private Config() { }

        public Config(string slnFilePath, string orgName, string xncfName, string version, string menuName, string icon, string description)
        {
            SlnFilePath = slnFilePath;
            OrgName = orgName;
            XncfName = xncfName;
            Version = version;
            MenuName = menuName;
            Icon = icon;
            Description = description;
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
