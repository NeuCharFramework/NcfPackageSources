/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysButton.cs
    File description: SysButton related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table("SysButtons")]
    public class SysButton : EntityBase<string>
    {
        public SysButton()
        {
            Id = Guid.NewGuid().ToString();
            AddTime = DateTime.Now;
            LastUpdateTime = AddTime;
        }

        public SysButton(SysButtonDto sysButtonDto) : this()
        {
            MenuId = sysButtonDto.MenuId;
            ButtonName = sysButtonDto.ButtonName;
            OpearMark = sysButtonDto.OpearMark;
            Url = sysButtonDto.Url;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(150)]
        public string MenuId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(150)]
        [Required]
        public string ButtonName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(150)]
        public string OpearMark { get; set; }

        public void Update(SysButtonDto item)
        {
            ButtonName = item.ButtonName;
            OpearMark = item.OpearMark;
            Url = item.Url;
            LastUpdateTime = DateTime.Now;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(500)]
        public string Url { get; set; }
    }
}
