using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.WorkContext
{
    /// <summary>
    /// Admin area context
    /// </summary>
    public class AdminWorkContext
    {
        /// <summary>
        /// Name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public int AdminUserId { get; set; }

        /// <summary>
        /// Owned roles
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }
    }
}
