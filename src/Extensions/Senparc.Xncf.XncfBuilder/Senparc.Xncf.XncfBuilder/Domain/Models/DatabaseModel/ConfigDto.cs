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
        ///Sln file path
        /// </summary>
        [MaxLength(300)]
        public string SlnPath { get; set; }
        /// <summary>
        ///organization name
        /// </summary>
        [MaxLength(300)]
        public string OrgName { get; set; }

        /// <summary>
        /// module name
        /// </summary>
        [MaxLength(50)]
        public string ModuleName { get; set; }

        /// <summary>
        /// version number
        /// </summary>
        [MaxLength(100)]
        public string Version { get; set; }

        /// <summary>
        ///menu name
        /// </summary>
        [MaxLength(100)]
        public string MenuName { get; set; }

        /// <summary>
        /// icon
        /// </summary>
        [MaxLength(100)]
        public string Icon { get; set; }

        private ConfigDto() { }

    }
}
