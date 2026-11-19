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
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminWorkContext
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// userId
        /// </summary>
        public int AdminUserId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }
    }
}
