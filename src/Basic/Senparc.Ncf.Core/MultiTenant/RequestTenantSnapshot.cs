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

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Config;
using System;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    ///Legacy comment translated to English.
    /// <para>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </para>
    /// </summary>
    public sealed class RequestTenantSnapshot
    {
        public int Id { get; }
        public string Name { get; }
        public string TenantKey { get; }
        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public static RequestTenantSnapshot Unmatched { get; } = new(0, null, null, false);

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// <para>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </para>
        /// </summary>
        ///Legacy comment translated to English.
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
