/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Config.cs
    File description: Config related implementation

    Created by: Senparc - 20210724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [DefaultValue(true)]
        public bool UseCategoryFilter { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [DefaultValue(true)]
        public bool Enabled { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AllowUserRoles { get; set; }
    }
}
