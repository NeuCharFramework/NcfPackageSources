/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AccountDto.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

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
        ///Legacy comment translated to English.
        /// </summary>
        public string ValidateCode { get; set; }
    }

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
        ///Legacy comment translated to English.
        /// </summary>
        public DateTimeOffset? TokenExpiresUtc { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }

        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuTreeItemDto> MenuTree { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public IEnumerable<string> PermissionCodes { get; set; }
        public string RealName { get; internal set; }
    }
}
