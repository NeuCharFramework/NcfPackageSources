/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysButtonDto.cs
    File description: SysButtonDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>Transfers administrative button permission values.</summary>
    public class SysButtonDto : DtoBase
    {

        //public bool IsDeleted { get; set; }

        public string Id { get; set; }

        /// <summary>Gets or sets the owning menu identifier.</summary>
        [MaxLength(50)]
        public string MenuId { get; set; }

        /// <summary>Gets or sets the button display name.</summary>
        [MaxLength(50)]
        //[Required]
        public string ButtonName { get; set; }

        /// <summary>Gets or sets the operation/resource marker.</summary>
        [MaxLength(50)]
        public string OpearMark { get; set; }

        /// <summary>Gets or sets the button URL or action route.</summary>
        [MaxLength(350)]
        public string Url { get; set; }
    }
}
