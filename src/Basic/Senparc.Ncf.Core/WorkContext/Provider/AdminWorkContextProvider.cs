/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminWorkContextProvider.cs
    File description: AdminWorkContextProvider related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Senparc.Ncf.Core.WorkContext.Provider
{
    /// <summary>
    /// Builds an <see cref="AdminWorkContext"/> from administrator claims in the
    /// current HTTP request.
    /// </summary>
    public class AdminWorkContextProvider : IAdminWorkContextProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a provider that reads administrator identity claims from the current HTTP context.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor for the current request context.</param>
        public AdminWorkContextProvider(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Creates an administrator work-context snapshot from name, identifier, and role claims.
        /// Missing claims result in default values rather than a fabricated authenticated identity.
        /// </summary>
        /// <returns>The current administrator context, or an empty context when no claims are available.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when the configured <see cref="IHttpContextAccessor"/> does not expose
        /// an HTTP context. Callers outside an active request should provide a suitable
        /// accessor or avoid resolving this provider.
        /// </exception>
        public AdminWorkContext GetAdminWorkContext()
        {
            AdminWorkContext adminWorkContext = new AdminWorkContext();

            adminWorkContext.UserName = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Name)?.Value;
            bool isConvertSucess = int.TryParse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier)?.Value, out int convertId);
            if (isConvertSucess)
            {
                adminWorkContext.AdminUserId = convertId;
            }
            adminWorkContext.RoleCodes = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Role)?.Value?.Split(",").ToList() ?? new List<string>();
            return adminWorkContext;
        }
    }
}
