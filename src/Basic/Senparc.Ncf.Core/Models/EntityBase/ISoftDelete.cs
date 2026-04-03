using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Database data soft deletion interface
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>
        /// Whether to soft delete
        /// </summary>
        bool Flag { get; set; }
    }
}
