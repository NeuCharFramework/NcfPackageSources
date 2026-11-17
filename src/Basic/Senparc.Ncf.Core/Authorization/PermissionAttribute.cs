/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PermissionAttribute.cs
    File description: PermissionAttribute related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
