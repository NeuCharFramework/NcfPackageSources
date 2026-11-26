/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AccountDto.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Models.Dto
{
    public class AccountDto
    {
    }

    /// <summary>
    /// Carries the credentials and optional tenant selector submitted to the administrator login endpoint.
    /// </summary>
    public class AccountLoginDto
    {
        [Required]
        [MaxLength(55)]
        public string UserName { get; set; }

        [MaxLength(55)]
        [Required]
        public string Password { get; set; }

        [MaxLength(100)]
        public string TenantKey { get; set; }

        /// <summary>
        /// Gets or sets the user-provided validation code required by the login flow when enabled.
        /// </summary>
        public string ValidateCode { get; set; }
    }

    /// <summary>
    /// Contains the authenticated administrator identity, token, and authorization data returned by login.
    /// </summary>
    public class AccountLoginResultDto
    {
        /// <summary>
        /// user name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Jwt token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the UTC instant at which <see cref="Token"/> expires.
        /// </summary>
        public DateTimeOffset? TokenExpiresUtc { get; set; }

        /// <summary>
        /// Gets or sets the role codes granted to the authenticated administrator.
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }

        /// <summary>
        /// Gets or sets the menu tree available to the authenticated administrator.
        /// </summary>
        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuTreeItemDto> MenuTree { get; set; }

        /// <summary>
        /// Gets or sets the permission/resource codes granted to the authenticated administrator.
        /// </summary>
        public IEnumerable<string> PermissionCodes { get; set; }
        /// <summary>
        /// Gets or sets the display name associated with the authenticated administrator.
        /// </summary>
        public string RealName { get; internal set; }
    }
}
