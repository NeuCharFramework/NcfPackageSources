/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PermissionRequirement.cs
    File description: PermissionRequirement related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    /// 
    /// <see cref="https://docs.microsoft.com/zh-cn/aspnet/core/security/authorization/resourcebased?view=aspnetcore-6.0#write-a-resource-based-handler"/>
    /// </summary>
    public class PermissionRequirement: IAuthorizationRequirement
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string All = "*";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string[] ResourceCodes { get; set; }

        public PermissionRequirement()
        {
            this.ResourceCodes = new string[] { All };
        }

        public PermissionRequirement(string[] resourceCodes)
        {
            ResourceCodes = resourceCodes.Where(code => !string.IsNullOrEmpty(code?.Trim())).Select(code => code.Trim()).ToArray(); //Legacy comment translated to English.
        }
    }
}
