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

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

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
    /// Provides shared administrator identity and role helpers for local
    /// application services.
    /// </summary>
    public class LocalAppServiceBase : AppServiceBase
    {
        /// <summary>
        /// Initializes the local application-service base with a request service provider.
        /// </summary>
        /// <param name="serviceProvider">The dependency-injection service provider.</param>
        public LocalAppServiceBase(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        /// <summary>
        /// Gets the current administrator identifier from the name-identifier claim.
        /// </summary>
        /// <param name="httpContext">
        /// Optional context to inspect. When omitted, the current context is resolved
        /// from <see cref="IHttpContextAccessor"/>.
        /// </param>
        /// <returns>The parsed administrator identifier, or <c>-1</c> when unavailable.</returns>
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
        /// Gets distinct administrator role codes from the current request claims.
        /// </summary>
        /// <param name="httpContext">
        /// Optional context to inspect. When omitted, the current context is resolved
        /// from <see cref="IHttpContextAccessor"/>.
        /// </param>
        /// <returns>A case-insensitive distinct list of trimmed role codes.</returns>
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
        /// Determines whether the current administrator has the configured
        /// super-administrator role code.
        /// </summary>
        /// <param name="httpContext">Optional request context to inspect.</param>
        /// <returns><see langword="true"/> when the administrator has the super-admin role.</returns>
        public bool IsCurrentAdminSuperAdmin(HttpContext httpContext = null)
        {
            return GetCurrentAdminRoleCodes(httpContext)
                .Any(role => string.Equals(role, Senparc.Ncf.Service.Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, StringComparison.OrdinalIgnoreCase));
        }

    }
}
