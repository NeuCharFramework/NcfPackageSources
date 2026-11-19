/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SenparcRequireHttpsAttribute.cs
    File description: SenparcRequireHttpsAttribute related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Ncf.Core.Config;

namespace Senparc.Mvc.Filter
{
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
          Justification = "Unsealed so that subclassed types can set properties in the default constructor or override our behavior.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class SenparcRequireHttpsAttribute: RequireHttpsAttribute
    {
        public override void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (SiteConfig.IsTestSite)
            {
                return;//Legacy comment translated to English.
            }

            base.OnAuthorization(filterContext);
        }
    }
}
