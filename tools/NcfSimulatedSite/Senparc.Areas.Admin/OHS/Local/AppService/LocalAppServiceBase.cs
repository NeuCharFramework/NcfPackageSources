/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LocalAppServiceBase.cs
    File description: LocalAppServiceBase.cs implementation and project behavior.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.XncfModuleManager.Domain.Services;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class LocalAppServiceBase : AppServiceBase
    {
        public LocalAppServiceBase(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public int GetCurrentAdminUserInfoId(HttpContext httpContext = null)
        {
            IEnumerable<Claim> claims = httpContext == null ? ServiceProvider.GetService<IHttpContextAccessor>().HttpContext.User.Claims : httpContext.User.Claims;
            bool isConvertSucess = int.TryParse(claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier)?.Value, out int convertId);
            if (isConvertSucess)
            {
                return convertId;
            }
            return -1;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<string> GetCurrentAdminRoleCodes(HttpContext httpContext = null)
        {
            IEnumerable<Claim> claims = httpContext == null ? ServiceProvider.GetService<IHttpContextAccessor>().HttpContext.User.Claims : httpContext.User.Claims;
            return claims
                .Where(c => c.Type == ClaimTypes.Role)
                .SelectMany(c => c.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsCurrentAdminSuperAdmin(HttpContext httpContext = null)
        {
            return GetCurrentAdminRoleCodes(httpContext)
                .Any(role => string.Equals(role, Senparc.Ncf.Service.Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, StringComparison.OrdinalIgnoreCase));
        }

    }
}
