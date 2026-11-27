/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Config.cs
    File description: Config related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

----------------------------------------------------------------*/

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
    [Table(Register.DATABASE_PREFIX + nameof(Config))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    /// <summary>Stores XNCF Builder solution, module identity, and display metadata.</summary>
    public class Config : EntityBase<int>
    {
        /// <summary>
        /// Sln filepath
        /// </summary>
        [MaxLength(300)]
        public string SlnFilePath { get; private set; }
        /// <summary>Gets the organization name used by generated files.</summary>
        [MaxLength(300)]
        public string OrgName { get; private set; }

        /// <summary>Gets the generated XNCF module name.</summary>
        [MaxLength(50)]
        public string XncfName { get; private set; }

        /// <summary>Gets the generated module version.</summary>
        [MaxLength(100)]
        public string Version { get; private set; }

        /// <summary>
        /// menu name
        /// </summary>
        [MaxLength(100)]
        public string MenuName { get; private set; }

        /// <summary>
        /// icon
        /// </summary>
        [MaxLength(100)]
        public string Icon { get; private set; }

        /// <summary>
        /// description
        /// </summary>
        [MaxLength(400)]
        public string Description { get; private set; }

        private Config() { }

        /// <summary>Initializes XNCF Builder configuration metadata.</summary>
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
