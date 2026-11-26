/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysButton.cs
    File description: SysButton related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>Stores one administrative button permission and its route.</summary>
    [Table("SysButtons")]
    public class SysButton : EntityBase<string>
    {
        /// <summary>
        /// Initializes a new button entity with identifier and timestamps.
        /// </summary>
        public SysButton()
        {
            Id = Guid.NewGuid().ToString();
            AddTime = DateTime.Now;
            LastUpdateTime = AddTime;
        }

        /// <summary>
        /// Initializes a button entity from an editable DTO.
        /// </summary>
        /// <param name="sysButtonDto">The button values to copy.</param>
        public SysButton(SysButtonDto sysButtonDto) : this()
        {
            MenuId = sysButtonDto.MenuId;
            ButtonName = sysButtonDto.ButtonName;
            OpearMark = sysButtonDto.OpearMark;
            Url = sysButtonDto.Url;
        }

        /// <summary>Gets or sets the owning menu identifier.</summary>
        [MaxLength(150)]
        public string MenuId { get; set; }

        /// <summary>Gets or sets the button display name.</summary>
        [MaxLength(150)]
        [Required]
        public string ButtonName { get; set; }

        /// <summary>Gets or sets the operation/resource marker.</summary>
        [MaxLength(150)]
        public string OpearMark { get; set; }

        /// <summary>
        /// Copies editable button fields from a DTO and refreshes the entity update timestamp.
        /// The associated menu identifier is intentionally not changed by this operation.
        /// </summary>
        /// <param name="item">The DTO containing the new button name, operation mark, and URL.</param>
        public void Update(SysButtonDto item)
        {
            ButtonName = item.ButtonName;
            OpearMark = item.OpearMark;
            Url = item.Url;
            LastUpdateTime = DateTime.Now;
        }

        /// <summary>Gets or sets the button URL or action route.</summary>
        [MaxLength(500)]
        public string Url { get; set; }
    }
}
