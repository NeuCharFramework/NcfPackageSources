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
    /// <summary>Identifies the source used to resolve the current tenant.</summary>
    public enum TenantRule
    {
        /// <summary>Resolves tenants from the request domain.</summary>
        DomainName = 0,
        /// <summary>Resolves tenants from a request header.</summary>
        RequestHeader = 1,
        /// <summary>Resolves tenants from login or explicit user input.</summary>
        LoginInput = 2,
        /// <summary>
        /// default
        /// </summary>
        Default = LoginInput
    }
}
