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
        /// 登录后即可访问
        /// </summary>
        public const string All = "*";

        /// <summary>
        /// 资源code
        /// </summary>
        public string[] ResourceCodes { get; set; }

        public PermissionRequirement()
        {
            this.ResourceCodes = new string[] { All };
        }

        public PermissionRequirement(string[] resourceCodes)
        {
            ResourceCodes = resourceCodes.Where(code => !string.IsNullOrEmpty(code?.Trim())).Select(code => code.Trim()).ToArray(); // 去前后空格
        }
    }
}
