/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantRule.cs
    File description: TenantRule related implementation

    Created by: Senparc - 20210212

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum TenantRule
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        DomainName = 0,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        RequestHeader = 1,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        LoginInput = 2,
        /// <summary>
        /// default
        /// </summary>
        Default = LoginInput
    }
}
