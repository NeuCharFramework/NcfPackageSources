using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Database Dto base class
    /// </summary>
    public class DtoBase<T> : DtoBase
        where T : struct
    {
        public T Id { get; set; }
    }

    /// <summary>
    /// Database Dto base class
    /// </summary>
    public class DtoBase<T, TID> : DtoBase
    where T : EntityBase<TID>
    {
        public TID Id { get; set; }
    }


    /// <summary>
    /// Database Dto base class
    /// </summary>
    public class DtoBase : IDtoBase
    {
        /// <summary>
        /// Whether to soft delete
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
        ///add time
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// last updated time
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        ///Tenant ID
        /// <para>If it is -1, multi-tenancy is not enabled in this system</para>
        /// <para>If it is 0, it is system public data (used in special circumstances)</para>
        /// </summary>
        public int TenantId { get; set; }
    }
}
