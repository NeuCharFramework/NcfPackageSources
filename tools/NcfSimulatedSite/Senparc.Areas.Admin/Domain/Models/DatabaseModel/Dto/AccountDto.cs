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
        /// Verification code
        /// </summary>
        public string ValidateCode { get; set; }
    }

    public class AccountLoginResultDto
    {
        /// <summary>
        /// username
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Jwt token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// role list
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }

        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuTreeItemDto> MenuTree { get; set; }

        /// <summary>
        ///Operation code
        /// </summary>
        public IEnumerable<string> PermissionCodes { get; set; }
        public string RealName { get; internal set; }
    }
}