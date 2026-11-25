/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminWorkContext.cs
    File description: AdminWorkContext related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.WorkContext
{
    /// <summary>
    /// Holds the administrator identity and role information used by the current work context.
    /// </summary>
    public class AdminWorkContext
    {
        /// <summary>
        /// Gets or sets the administrator's display or login name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the administrator account identifier.
        /// </summary>
        public int AdminUserId { get; set; }

        /// <summary>
        /// Gets or sets the role codes granted to the administrator.
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }
    }
}
