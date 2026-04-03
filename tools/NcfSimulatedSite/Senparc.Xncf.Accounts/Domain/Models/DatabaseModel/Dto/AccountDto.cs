using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.Accounts.Domain.Models.Dto
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
        /// role associated with user
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }
    }
}
