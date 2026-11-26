/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PermissionRequirement.cs
    File description: PermissionRequirement related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    /// Authorization requirement containing the resource codes accepted by a permission handler.
    /// </summary>
    public class PermissionRequirement: IAuthorizationRequirement
    {
        /// <summary>
        /// Gets the wildcard resource code that represents unrestricted permission.
        /// </summary>
        public const string All = "*";

        /// <summary>
        /// Gets or sets the normalized resource codes required by the authorization handler.
        /// </summary>
        public string[] ResourceCodes { get; set; }

        /// <summary>
        /// Initializes a requirement that grants the wildcard <see cref="All"/> code.
        /// </summary>
        public PermissionRequirement()
        {
            this.ResourceCodes = new string[] { All };
        }

        /// <summary>
        /// Initializes a requirement with non-empty normalized resource codes.
        /// </summary>
        /// <param name="resourceCodes">The resource codes to require.</param>
        public PermissionRequirement(string[] resourceCodes)
        {
            ResourceCodes = resourceCodes.Where(code => !string.IsNullOrEmpty(code?.Trim())).Select(code => code.Trim()).ToArray(); //Legacy comment translated to English.
        }
    }
}
