/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PermissionAttribute.cs
    File description: PermissionAttribute related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public class PermissionAttribute : TypeFilterAttribute
    {
        //public string[] Codes { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        public PermissionAttribute(string codes)
            : base(typeof(PermissionFilterAttribute))
        {
            //Arguments = new[] { new PermissionRequirement(Codes) };
            Arguments = new[] { new PermissionRequirement(codes.Split(",")) };
        }
    }
}
