using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.Swagger.Models.DataBaseModel
{
    /// <summary>
    ///config
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Config))]//The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class Config : EntityBase<int>
    {
        /// <summary>
        /// Use directory filtering
        /// </summary>
        [DefaultValue(true)]
        public bool UseCategoryFilter { get; set; }
        /// <summary>
        ///enable
        /// </summary>
        [DefaultValue(true)]
        public bool Enabled { get; set; }
        /// <summary>
        /// User group allowed to access, leave blank and no judgment will be made
        /// </summary>
        public string AllowUserRoles { get; set; }
    }
}
