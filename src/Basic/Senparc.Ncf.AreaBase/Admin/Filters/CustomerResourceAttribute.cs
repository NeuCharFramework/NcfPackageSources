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
    ///Do not perform permission verification
    /// </summary>
    public class IgnoreAuthAttribute: Attribute, IFilterMetadata
    {

    }
}
