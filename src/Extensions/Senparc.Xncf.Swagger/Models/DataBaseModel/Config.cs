/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Config.cs
    File description: Config related implementation

    Created by: Senparc - 20210724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.Swagger.Models.DataBaseModel
{
    /// <summary>
    /// configuration
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Config))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class Config : EntityBase<int>
    {
        /// <summary>Gets or sets whether category filtering is enabled.</summary>
        [DefaultValue(true)]
        public bool UseCategoryFilter { get; set; }
        /// <summary>Gets or sets whether the Swagger configuration is enabled.</summary>
        [DefaultValue(true)]
        public bool Enabled { get; set; }
        /// <summary>Gets or sets the comma-separated roles allowed to use the Swagger surface.</summary>
        public string AllowUserRoles { get; set; }
    }
}
