/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DtoBase.cs

    File description: DtoBase related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class DtoBase<T> : DtoBase
        where T : struct
    {
        public T Id { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class DtoBase<T, TID> : DtoBase
    where T : EntityBase<TID>
    {
        public TID Id { get; set; }
    }


    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class DtoBase : IDtoBase
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Flag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(150)]
        public string AdminRemark { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(150)]
        public string Remark { get; set; }

        /// <summary>
        /// creation time
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public int TenantId { get; set; }
    }
}
