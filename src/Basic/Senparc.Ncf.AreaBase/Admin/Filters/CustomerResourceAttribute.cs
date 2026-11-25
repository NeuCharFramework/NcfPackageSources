/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CustomerResourceAttribute.cs
    File description: CustomerResourceAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.12 Improved administration page and authorization support

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Senparc.Ncf.AreaBase.Admin.Filters
{
    /// <summary>
    /// Associates a handler with the resource codes used by administration permission checks.
    /// </summary>
    public class CustomerResourceAttribute : Attribute
    {
        public string[] ResourceCodes { get; set; }

        /// <summary>
        /// Initializes resource-code metadata for a handler.
        /// </summary>
        /// <param name="resuouceCodes">The resource codes required by the handler.</param>
        public CustomerResourceAttribute(params string[] resuouceCodes)
        {
            ResourceCodes = resuouceCodes;
        }
    }

    /// <summary>
    /// Do not perform permission verification
    /// </summary>
    public class IgnoreAuthAttribute: Attribute, IFilterMetadata
    {

    }
}
