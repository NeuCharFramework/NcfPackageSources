/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysButtonDto.cs
    File description: SysButtonDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysButtonDto : DtoBase
    {

        //public bool IsDeleted { get; set; }

        public string Id { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(50)]
        public string MenuId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(50)]
        //[Required]
        public string ButtonName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(50)]
        public string OpearMark { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(350)]
        public string Url { get; set; }
    }
}
