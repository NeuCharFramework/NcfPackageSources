/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RequestTenantSnapshot.cs
    File description: Related implementation.
    Change description: vNext Updated module behavior and related support.

    Created by: Senparc - 20261028

    Modified by: Senparc - 20261028
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.30.3-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Config;
using System;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// Immutable tenant context captured from a request and safe to restore into another scope.
    /// </summary>
    public sealed class RequestTenantSnapshot
    {
        public int Id { get; }
        public string Name { get; }
        public string TenantKey { get; }
        /// <summary>
        /// Indicates whether tenant matching succeeded when this snapshot was captured.
        /// </summary>
        public bool MatchSuccess { get; }

        private RequestTenantSnapshot(int id, string name, string tenantKey, bool matchSuccess)
        {
            Id = id;
            Name = name;
            TenantKey = tenantKey;
            MatchSuccess = matchSuccess;
        }

        /// <summary>
        /// Gets a snapshot that intentionally carries no matched tenant.
        /// </summary>
        public static RequestTenantSnapshot Unmatched { get; } = new(0, null, null, false);

        /// <summary>
        /// Captures the matched tenant from a request service provider.
        /// </summary>
        /// <param name="serviceProvider">The request or scope service provider.</param>
        /// <returns>A matched snapshot, or <see cref="Unmatched"/> when no valid tenant is available.</returns>
        public static RequestTenantSnapshot Capture(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                return Unmatched;
            }

            RequestTenantInfo requestTenantInfo;
            try
            {
                requestTenantInfo = serviceProvider.GetService<RequestTenantInfo>();
            }
            catch (ObjectDisposedException)
            {
                return Unmatched;
            }

            if (requestTenantInfo == null || !requestTenantInfo.MatchSuccess)
            {
                return Unmatched;
            }

            return new RequestTenantSnapshot(
                requestTenantInfo.Id,
                requestTenantInfo.Name,
                requestTenantInfo.TenantKey,
                requestTenantInfo.MatchSuccess);
        }

        /// <summary>
        /// Creates a snapshot from tenant information when matching succeeded.
        /// </summary>
        /// <param name="info">The tenant information to copy.</param>
        /// <returns>A matched snapshot, or <see cref="Unmatched"/> for invalid information.</returns>
        public static RequestTenantSnapshot From(RequestTenantInfo info)
        {
            if (info == null || !info.MatchSuccess)
            {
                return Unmatched;
            }

            return new RequestTenantSnapshot(
                info.Id,
                info.Name,
                info.TenantKey,
                info.MatchSuccess);
        }

        /// <summary>
        /// Restores the matched tenant into a dependency-injection scope when multi-tenancy is enabled.
        /// </summary>
        /// <param name="scopeServiceProvider">The target scope service provider.</param>
        public void Restore(IServiceProvider scopeServiceProvider)
        {
            if (scopeServiceProvider == null
                || !SiteConfig.SenparcCoreSetting.EnableMultiTenant
                || !MatchSuccess)
            {
                return;
            }

            var requestTenantInfo = scopeServiceProvider.GetRequiredService<RequestTenantInfo>();
            requestTenantInfo.Id = Id;
            requestTenantInfo.Name = Name;
            requestTenantInfo.TenantKey = TenantKey;
            requestTenantInfo.TryMatch(true);
        }
    }
}
