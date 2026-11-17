/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IDtoBase.cs

    File description: IDtoBase related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Routing.Tree;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IDtoBase<T> : IDtoBase
    {
        T Id { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IDtoBase
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        bool Flag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(150)]
        string AdminRemark { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(150)]
        string Remark { get; set; }

        /// <summary>
        /// creation time
        /// </summary>
        DateTime AddTime { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        DateTime LastUpdateTime { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public int TenantId { get; set; }
    }
}
