/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminAuthConfigService.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class AdminAuthExpireSettings
    {
        public int AdminWebLoginExpireMinutes { get; set; }
        public int BackendJwtExpireMinutes { get; set; }
        public bool UsingDefault { get; set; }
        public string Source { get; set; }
    }

    public class AdminAuthConfigService : BaseClientService<AdminAuthConfig>
    {
        public const int MinExpireMinutes = 5;
        public const int MaxExpireMinutes = 60 * 24 * 365; // 1 year

        public AdminAuthConfigService(IAdminAuthConfigRepository repository, IServiceProvider serviceProvider)
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        /// Classifies provider exception text that indicates the auth-config table
        /// has not yet been created.
        /// </summary>
        /// <param name="ex">The database exception to inspect.</param>
        /// <returns><see langword="true"/> when known missing-table markers are present.</returns>
        private static bool IsTableMissingException(Exception ex)
        {
            var message = ex?.ToString() ?? string.Empty;
            var keywords = new[]
            {
                "no such table",
                "invalid object name",
                "doesn't exist",
                "does not exist",
                "relation",
                "ora-00942",
                "对象名无效",
                "表或视图不存在",
                "找不到对象",
                "表不存在"
            };
            return keywords.Any(k => message.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Clamps an authentication expiration setting to the supported range.
        /// </summary>
        /// <param name="expireMinutes">The configured duration.</param>
        /// <param name="defaultValue">The fallback when the configured value is non-positive.</param>
        /// <returns>The normalized duration in minutes.</returns>
        public int NormalizeExpireMinutes(int expireMinutes, int defaultValue)
        {
            var value = expireMinutes <= 0 ? defaultValue : expireMinutes;
            if (value < MinExpireMinutes)
            {
                return MinExpireMinutes;
            }

            if (value > MaxExpireMinutes)
            {
                return MaxExpireMinutes;
            }

            return value;
        }

        /// <summary>
        /// Loads effective auth expiration settings, falling back to defaults when
        /// the record/table is unavailable or another read error occurs.
        /// </summary>
        /// <returns>The effective settings and their source marker.</returns>
        public AdminAuthExpireSettings GetEffectiveExpireSettings()
        {
            try
            {
                var config = GetObject(z => true);
                if (config == null)
                {
                    return GetDefaultSettings("missing-record");
                }

                return new AdminAuthExpireSettings
                {
                    AdminWebLoginExpireMinutes = NormalizeExpireMinutes(config.AdminWebLoginExpireMinutes, AdminAuthConfig.DefaultAdminWebLoginExpireMinutes),
                    BackendJwtExpireMinutes = NormalizeExpireMinutes(config.BackendJwtExpireMinutes, AdminAuthConfig.DefaultBackendJwtExpireMinutes),
                    UsingDefault = false,
                    Source = "table"
                };
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return GetDefaultSettings("missing-table");
            }
            catch
            {
                //Legacy comment translated to English.
                return GetDefaultSettings("fallback-on-exception");
            }
        }

        /// <summary>
        /// Creates default expiration settings with the specified diagnostic source.
        /// </summary>
        /// <param name="source">The reason the defaults were selected.</param>
        /// <returns>Default auth expiration settings.</returns>
        private static AdminAuthExpireSettings GetDefaultSettings(string source)
        {
            return new AdminAuthExpireSettings
            {
                AdminWebLoginExpireMinutes = AdminAuthConfig.DefaultAdminWebLoginExpireMinutes,
                BackendJwtExpireMinutes = AdminAuthConfig.DefaultBackendJwtExpireMinutes,
                UsingDefault = true,
                Source = source
            };
        }

        public async Task<(bool Success, string Message, AdminAuthExpireSettings Settings)> TrySaveExpireSettingsAsync(
            int adminWebLoginExpireMinutes,
            int backendJwtExpireMinutes)
        {
            var normalizedWeb = NormalizeExpireMinutes(adminWebLoginExpireMinutes, AdminAuthConfig.DefaultAdminWebLoginExpireMinutes);
            var normalizedJwt = NormalizeExpireMinutes(backendJwtExpireMinutes, AdminAuthConfig.DefaultBackendJwtExpireMinutes);

            AdminAuthConfig config;
            try
            {
                config = await GetObjectAsync(z => true).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return (false,
                    $"认证配置表 {Register.DATABASE_PREFIX}{nameof(AdminAuthConfig)} 不存在，请先完成数据库升级（EF Core Merge）。",
                    GetDefaultSettings("missing-table"));
            }

            if (config == null)
            {
                config = new AdminAuthConfig(normalizedWeb, normalizedJwt);
            }
            else
            {
                config.Update(normalizedWeb, normalizedJwt);
            }

            await SaveObjectAsync(config).ConfigureAwait(false);

            return (true,
                $"已保存：网页登录持续时间={normalizedWeb} 分钟，JWT 过期时间={normalizedJwt} 分钟。",
                new AdminAuthExpireSettings
                {
                    AdminWebLoginExpireMinutes = normalizedWeb,
                    BackendJwtExpireMinutes = normalizedJwt,
                    UsingDefault = false,
                    Source = "table"
                });
        }
    }
}
