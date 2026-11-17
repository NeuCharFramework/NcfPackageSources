/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CustomerResourceAttribute.cs
    File description: CustomerResourceAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Senparc.Ncf.AreaBase.Admin.Filters
{
    public class CustomerResourceAttribute : Attribute
    {
        public string[] ResourceCodes { get; set; }

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
