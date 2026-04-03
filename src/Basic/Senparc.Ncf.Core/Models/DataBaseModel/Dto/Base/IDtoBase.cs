using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Routing.Tree;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Base class for all DTO interfaces or classes
    /// </summary>
    public interface IDtoBase<T> : IDtoBase
    {
        T Id { get; set; }
    }

    /// <summary>
    /// Base class for all DTO interfaces or classes
    /// </summary>
    public interface IDtoBase
    {
        /// <summary>
        /// Whether to soft delete
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
        ///add time
        /// </summary>
        DateTime AddTime { get; set; }
        /// <summary>
        /// last updated time
        /// </summary>
        DateTime LastUpdateTime { get; set; }

        /// <summary>
        ///Tenant ID
        /// <para>If it is -1, multi-tenancy is not enabled in this system</para>
        /// <para>If it is 0, it is system public data (used in special circumstances)</para>
        /// </summary>
        public int TenantId { get; set; }
    }
}
