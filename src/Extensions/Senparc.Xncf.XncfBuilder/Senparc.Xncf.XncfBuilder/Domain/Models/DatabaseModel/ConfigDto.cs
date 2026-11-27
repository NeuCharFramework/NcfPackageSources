/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConfigDto.cs
    File description: ConfigDto related implementation

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
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.XncfBuilder
{
    /// <summary>Transfers XNCF Builder solution and generated-module settings.</summary>
    public class ConfigDto : DtoBase
    {
        /// <summary>
        /// Sln filepath
        /// </summary>
        [MaxLength(300)]
        public string SlnPath { get; set; }
        /// <summary>Gets or sets the organization name.</summary>
        [MaxLength(300)]
        public string OrgName { get; set; }

        /// <summary>Gets or sets the generated module name.</summary>
        [MaxLength(50)]
        public string ModuleName { get; set; }

        /// <summary>Gets or sets the module version.</summary>
        [MaxLength(100)]
        public string Version { get; set; }

        /// <summary>
        /// menu name
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
