/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: AdminUserInfo_Request.cs

    File description: Related implementation.

    

    

    Created by: Senparc - 20241028

    

    Modified by: Senparc - 20260724

    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface



----------------------------------------------------------------*/
using Senparc.Ncf.XncfBase.FunctionRenders;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.PL
{
    public class AdminUserInfo_LoginRequest : FunctionAppRequestBase
    {
        [Required]
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class AdminUserInfo_AddRoleRequest
    {
        /// <summary>
        /// roleId
        /// </summary>
        [Required]
        public IEnumerable<string> RoleId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int AccountId { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminUserInfo_CreateOrUpdateRequest : IValidatableObject
    {
        /// <summary>
        /// user name
        /// </summary>
        [Required]
        [MaxLength(35)]
        [MinLength(5)]
        public string UserName
        {
            get;
            set;
        }

        [MaxLength(30)]
        [MinLength(6)]
        public string Password
        {
            get;
            set;
        }

        [MaxLength(555)]
        public string Note
        {
            get;
            set;
        }

        [MaxLength(35)]
        public string RealName
        {
            get;
            set;
        }

        [MaxLength(25)]
        public string Phone
        {
            get;
            set;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(150)]
        public string AdminRemark
        {
            get;
            set;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(150)]
        public string Remark
        {
            get;
            set;
        }

        public int Id { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Password) && Id == 0)
            {
                var localizer = validationContext.GetService(typeof(IStringLocalizer<AdminResource>)) as IStringLocalizer<AdminResource>;
                yield return new ValidationResult(localizer?["AdminUserInfo.PasswordRequired"].Value ?? "密码为必填项！", new[] { "Password" });
            }
        }
    }
}
