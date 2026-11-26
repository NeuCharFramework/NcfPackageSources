/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminUserInfoService.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20260724
    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20260808
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add image captcha for admin login (required after 2 failed attempts)

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain
{
    public class AdminUserInfoService : BaseClientService<AdminUserInfo>
    {
        private const int MinExpireMinutes = 5;
        private const int MaxExpireMinutes = 60 * 24 * 365; // 1 year

        private readonly Lazy<IHttpContextAccessor> _contextAccessor;
        private readonly Lazy<AdminAuthConfigService> _adminAuthConfigService;

        public AdminUserInfoService(IAdminUserInfoRepository repository,
            Lazy<IHttpContextAccessor> httpContextAccessor,
            Lazy<AdminAuthConfigService> adminAuthConfigService,
            IServiceProvider serviceProvider)
            : base(repository, serviceProvider)
        {
            _contextAccessor = httpContextAccessor;
            _adminAuthConfigService = adminAuthConfigService;
        }

        /// <summary>
        /// Clamps an authentication expiration setting to the supported five-minute
        /// through one-year range, using a default when the configured value is non-positive.
        /// </summary>
        /// <param name="expireMinutes">The configured duration in minutes.</param>
        /// <param name="defaultValue">The fallback duration.</param>
        /// <returns>The supported expiration duration.</returns>
        private int NormalizeExpireMinutes(int expireMinutes, int defaultValue)
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
        /// Returns the effective administrator web-login cookie lifetime in minutes.
        /// </summary>
        /// <returns>A clamped cookie lifetime.</returns>
        public int GetAdminWebLoginExpireMinutes()
        {
            var settings = _adminAuthConfigService.Value.GetEffectiveExpireSettings();
            return NormalizeExpireMinutes(settings.AdminWebLoginExpireMinutes,
                AdminAuthConfig.DefaultAdminWebLoginExpireMinutes);
        }

        /// <summary>
        /// Returns the effective backend JWT lifetime in minutes.
        /// </summary>
        /// <returns>A clamped JWT lifetime.</returns>
        public int GetBackendJwtExpireMinutes()
        {
            var settings = _adminAuthConfigService.Value.GetEffectiveExpireSettings();
            return NormalizeExpireMinutes(settings.BackendJwtExpireMinutes,
                AdminAuthConfig.DefaultBackendJwtExpireMinutes);
        }

        /// <summary>
        /// Reissues the administrator authentication cookie for the effective web-login
        /// lifetime while preserving the current claims.
        /// </summary>
        /// <param name="principal">The currently authenticated principal.</param>
        /// <returns>The UTC expiration time assigned to the renewed cookie.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the principal is not authenticated.</exception>
        public async Task<DateTimeOffset> KeepCookieLoginAliveAsync(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                throw new NcfExceptionBase("当前用户未登录，无法续期。");
            }

            var identity = new ClaimsIdentity(principal.Claims, SiteConfig.NcfAdminAuthorizeScheme);
            var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(GetAdminWebLoginExpireMinutes());
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = expiresUtc,
                IssuedUtc = DateTimeOffset.UtcNow,
                IsPersistent = false
            };

            await _contextAccessor.Value.HttpContext.SignInAsync(SiteConfig.NcfAdminAuthorizeScheme, new ClaimsPrincipal(identity), authProperties);
            return expiresUtc;
        }

        /// <summary>
        /// Reads a JWT expiration claim from a principal, accepting Unix seconds
        /// and parseable date-time text while returning <see langword="null"/> for invalid input.
        /// </summary>
        /// <param name="principal">The principal whose claims are inspected.</param>
        /// <returns>The normalized UTC expiration, or <see langword="null"/> when absent or invalid.</returns>
        public DateTimeOffset? TryGetJwtExpiresUtc(ClaimsPrincipal principal)
        {
            var expValue = principal?.Claims?
                .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp || c.Type == "exp" || c.Type == ClaimTypes.Expiration)
                ?.Value;

            if (string.IsNullOrWhiteSpace(expValue))
            {
                return null;
            }

            if (long.TryParse(expValue, out var expUnix))
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeSeconds(expUnix);
                }
                catch
                {
                    return null;
                }
            }

            if (DateTimeOffset.TryParse(expValue, out var expDateTimeOffset))
            {
                return expDateTimeOffset.ToUniversalTime();
            }

            return null;
        }

        /// <summary>
        /// Determines whether another administrator already uses the supplied user name.
        /// Comparison is case-insensitive after trimming.
        /// </summary>
        /// <param name="id">The current record identifier excluded from the duplicate check.</param>
        /// <param name="userName">The user name to test.</param>
        /// <returns><see langword="true"/> when another record has the same user name.</returns>
        public bool CheckUserNameExisted(long id, string userName)
        {
            userName = userName.Trim().ToUpper();

            return
            GetObject(
                z => z.Id != id && z.UserName.ToUpper() == userName /*z.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase)*/) != null;
        }

        ///Legacy comment translated to English.
        ////[ApiBind]
        ////[Core.BackendJwtAuthorize]
        /// <summary>
        /// Loads an administrator by its trimmed user name.
        /// </summary>
        /// <param name="userName">The user name to look up.</param>
        /// <returns>The matching administrator, or <see langword="null"/> when no record exists.</returns>
        public async Task<AdminUserInfo> GetUserInfoAsync(string userName)
        {
            var obj = await base.GetObjectAsync(z => z.UserName.Equals(userName.Trim()));
            return obj;
        }


        /// <summary>
        /// Signs out the current administrator using the configured NCF administration scheme.
        /// Sign-out failures are logged and suppressed.
        /// </summary>
        /// <returns>A task that completes after the sign-out attempt.</returns>
        public async Task LogoutAsync()
        {
            try
            {
                await _contextAccessor.Value.HttpContext.SignOutAsync(SiteConfig.NcfAdminAuthorizeScheme);
            }
            catch (Exception ex)
            {
                LogUtility.AdminUserInfo.ErrorFormat("退出登录失败。", ex);
            }
        }

        /// <summary>
        /// Creates the authenticated administrator claims principal and signs it in
        /// with the configured expiration window.
        /// </summary>
        /// <param name="userInfo">The administrator identity to authenticate.</param>
        /// <param name="rememberMe">Whether the caller requested persistent login state.</param>
        /// <param name="tenantKey">The optional tenant key included in tenant-aware claims.</param>
        /// <returns>A task that completes after the authentication cookie is issued.</returns>
        public virtual async Task LoginAsync(AdminUserInfo userInfo, bool rememberMe, string tenantKey = null)
        {
            #region Legacy section

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userInfo.UserName),
                new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString(), ClaimValueTypes.Integer),
                new Claim(NcfAuthorizationPolicyNames.AdminMemberClaim, "1", ClaimValueTypes.String)
            };

            var roleCodes = await GetRoleCodesAsync(userInfo.Id);
            if (roleCodes.Count > 0)
            {
                claims.Add(new Claim(ClaimTypes.Role, string.Join(",", roleCodes), ClaimValueTypes.String));
            }

            if (userInfo.TenantId > 0)
            {
                claims.Add(new Claim("TenantKey", tenantKey, ClaimValueTypes.String));
            }

            var identity = new ClaimsIdentity(SiteConfig.NcfAdminAuthorizeScheme);
            identity.AddClaims(claims);
            var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(GetAdminWebLoginExpireMinutes());
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = expiresUtc,
                IssuedUtc = DateTimeOffset.UtcNow,
                IsPersistent = false,
            };

            await LogoutAsync(); //Legacy comment translated to English.
            await _contextAccessor.Value.HttpContext.SignInAsync(SiteConfig.NcfAdminAuthorizeScheme, new ClaimsPrincipal(identity), authProperties);

            #endregion
        }

        //public bool CheckPassword(string userName, string password)
        //{
        //    var userInfo = GetObject(z => z.UserName.Equals(userName));
        //    if (userInfo == null)
        //    {
        //        return false;
        //    }
        //    var codedPassword = GetPassword(password, userInfo.PasswordSalt, false);
        //    return userInfo.Password == codedPassword;
        //}

        /// <summary>
        /// Reads the cached failed-login count for a user name.
        /// </summary>
        /// <param name="userName">The user name whose failed attempts are queried.</param>
        /// <returns>The cached failed-login count, or zero when no count is stored.</returns>
        public async Task<int> GetFailedLoginCountAsync(string userName)
        {
            var cache = CO2NET.Cache.CacheStrategyFactory.GetObjectCacheStrategyInstance();
            var failedCount = await cache.GetAsync<int>($"FailedLogin:{userName}");
            return failedCount;
        }

        /// <summary>
        /// Validates a candidate password under the cache lock, updates failed-attempt state,
        /// and signs in the administrator on success.
        /// </summary>
        /// <param name="adminUserInfo">The persisted administrator being authenticated.</param>
        /// <param name="password">The candidate password.</param>
        /// <param name="rememberMe">Whether the login should be persistent.</param>
        /// <param name="tenantKey">The optional tenant key for the login claims.</param>
        /// <returns>The authenticated administrator on success; otherwise <see langword="null"/>.</returns>
        /// <exception cref="LoginLockException">Thrown when the account is currently locked or becomes locked.</exception>
        public async Task<AdminUserInfo> TryLoginAsync(AdminUserInfo adminUserInfo, string password,
            bool rememberMe, string tenantKey = null)
        {
            var cache = CO2NET.Cache.CacheStrategyFactory.GetObjectCacheStrategyInstance();
            var lockTimeKey = $"LockTime:{adminUserInfo.UserName}";
            var failedLoginKey = $"FailedLogin:{adminUserInfo.UserName}";
            using (var cacheLock = await cache.BeginCacheLockAsync("LoginLock-Admin", adminUserInfo.UserName))
            {
                //Legacy comment translated to English.
                var lockEndTime = await cache.GetAsync<DateTime?>(lockTimeKey);
                if (lockEndTime.HasValue && lockEndTime.Value > DateTime.Now)
                {
                    throw new LoginLockException(lockEndTime.Value);
                }

                if (adminUserInfo.CheckPassword(password))
                {
                    //Legacy comment translated to English.
                    await cache.RemoveFromCacheAsync(failedLoginKey);
                    await cache.RemoveFromCacheAsync(lockTimeKey);

                    //Legacy comment translated to English.
                    await LoginAsync(adminUserInfo, rememberMe, tenantKey);
                    return adminUserInfo;
                }
                else
                {
                    //Legacy comment translated to English.
                    var failedCount = await cache.GetAsync<int>(failedLoginKey);
                    failedCount++;

                    //Legacy comment translated to English.
                    DateTime? lockTime = null;
                    if (failedCount >= 20)
                    {
                        lockTime = DateTime.Now.AddHours(failedCount * 2);
                    }
                    else if (failedCount >= 10)
                    {
                        lockTime = DateTime.Now.AddHours(2);
                    }
                    else if (failedCount >= 7)
                    {
                        lockTime = DateTime.Now.AddMinutes(30);
                    }
                    else if (failedCount >= 5)
                    {
                        lockTime = DateTime.Now.AddMinutes(5);
                    }

                    //Legacy comment translated to English.
                    await cache.SetAsync(failedLoginKey, failedCount, TimeSpan.FromHours(24));
                    if (lockTime.HasValue)
                    {
                        await cache.SetAsync(lockTimeKey, lockTime.Value, TimeSpan.FromHours(24));
                        throw new LoginLockException(lockTime.Value, showWrongLoginMessage: true);
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// Creates and persists a new administrator from the supplied DTO.
        /// </summary>
        /// <param name="objDto">The administrator creation values.</param>
        /// <returns>The newly persisted administrator.</returns>
        public async Task<AdminUserInfo> CreateAdminUserInfoAsync(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            var obj = new AdminUserInfo(objDto);
            await SaveObjectAsync(obj);
            return obj;
        }

        /// <summary>
        /// Updates and persists an existing administrator identified by the DTO.
        /// </summary>
        /// <param name="objDto">The administrator identifier and replacement values.</param>
        /// <returns>The updated administrator.</returns>
        /// <exception cref="Exception">Thrown when the administrator identifier does not exist.</exception>
        public async Task<AdminUserInfo> UpdateAdminUserInfoAsync(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            var obj = this.GetObject(z => z.Id == objDto.Id);
            if (obj == null)
            {
                throw new Exception("用户信息不存在！");
            }
            obj.UpdateObject(objDto);
            await SaveObjectAsync(obj);
            return obj;
        }

        /// <summary>
        /// Synchronously updates and persists an existing administrator.
        /// </summary>
        /// <param name="objDto">The administrator identifier and replacement values.</param>
        /// <exception cref="Exception">Thrown when the administrator identifier does not exist.</exception>
        public void UpdateAdminUserInfo(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            var obj = this.GetObject(z => z.Id == objDto.Id);
            if (obj == null)
            {
                throw new Exception("用户信息不存在！");
            }
            obj.UpdateObject(objDto);
            SaveObject(obj);
        }

        /// <summary>
        /// Maps one persisted administrator to the create/update DTO used by the editor.
        /// </summary>
        /// <param name="id">The administrator identifier.</param>
        /// <param name="includes">Optional related entities to include during lookup.</param>
        /// <returns>The mapped DTO, or a DTO with default values when mapping receives no entity.</returns>
        public CreateOrUpdate_AdminUserInfoDto GetAdminUserInfo(int id, params string[] includes)
        {
            var obj = GetObject(z => z.Id == id, includes: includes);

            var objDto = base.Mapper.Map<CreateOrUpdate_AdminUserInfoDto>(obj);

            return objDto;
        }

        /// <summary>
        /// Loads administrators whose identifiers are in the supplied list.
        /// </summary>
        /// <param name="ids">The administrator identifiers to retrieve.</param>
        /// <param name="includes">Optional related entities to include.</param>
        /// <returns>The matching administrators ordered by identifier.</returns>
        public List<AdminUserInfo> GetAdminUserInfo(List<int> ids, params string[] includes)
        {
            return GetFullList(z => ids.Contains(z.Id), z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, includes: includes);
        }


        /// <summary>
        /// Counts administrators belonging to a tenant while bypassing the context query filter.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>The number of administrators assigned to the tenant.</returns>
        public async Task<int> GetTenantUserCountAsync(int tenantId)
        {
            var db = BaseData.BaseDB.BaseDataContext;
            return await db.Set<AdminUserInfo>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .CountAsync(z => z.TenantId == tenantId);
        }

        /// <summary>
        /// Initializes the first administrator when no administrator record exists.
        /// </summary>
        /// <param name="userName">The optional initial user name.</param>
        /// <returns>The initialized administrator and generated password, or null values when initialization is already complete.</returns>
        public async Task<(AdminUserInfo AdminUserInfo, string Password)> InitAsync(string userName)
        {
            var oldAdminUserInfo = GetObject(z => true);
            if (oldAdminUserInfo != null)
            {
                return (null, null);
            }

            var adminUserInfoDto = new CreateOrUpdate_AdminUserInfoDto()
            {
                UserName = userName?.Trim(),
                Password = null,
                Note = "系统初始化账号"
            };


            var adminUserInfo = new AdminUserInfo(adminUserInfoDto);
            SaveObject(adminUserInfo);

            return (adminUserInfo, adminUserInfoDto.Password);
        }

        /// <summary>
        /// Loads and deletes an administrator record by identifier.
        /// </summary>
        /// <param name="id">The administrator record identifier.</param>
        /// <exception cref="Exception">Thrown when no matching administrator exists.</exception>
        public void DeleteObject(int id)
        {
            var obj = GetObject(z => z.Id == id);
            if (obj == null)
            {
                throw new Exception("用户信息不存在！");
            }
            DeleteObject(obj);
        }

        public override void SaveObject(AdminUserInfo obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("AdminUserInfo{2}：{0}（ID：{1}）", obj.UserName, obj.Id, isInsert ? "新增" : "编辑");
        }

        public override void DeleteObject(AdminUserInfo obj)
        {
            base.DeleteObject(obj);
            LogUtility.WebLogger.InfoFormat("AdminUserInfo被删除：{0}（ID：{1}）", obj.UserName, obj.Id);
        }

        /// <summary>
        /// Authenticates credentials and returns the JWT-oriented login result with roles,
        /// permissions, menu tree, and token expiration metadata.
        /// </summary>
        /// <param name="loginDto">The login credentials and optional tenant key.</param>
        /// <returns>The populated login result.</returns>
        /// <exception cref="LoginLockException">Propagated when the account is locked.</exception>
        //[ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AccountLoginResultDto> LoginAsync(AccountLoginDto loginDto)
        {
            AccountLoginResultDto result = new AccountLoginResultDto();
            string token;

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            Senparc.Xncf.Tenant.Domain.DataBaseModel.TenantInfo tenantInfo = null;
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant && !string.IsNullOrWhiteSpace(loginDto.TenantKey))
            {
                var tenantInfoService = _serviceProvider.GetService<Senparc.Xncf.Tenant.Domain.Services.TenantInfoService>();
                tenantInfo = tenantInfoService == null
                    ? null
                    : await tenantInfoService.GetObjectAsync(z => z.Enable && z.TenantKey.ToUpper() == loginDto.TenantKey.ToUpper());
                if (tenantInfo == null)
                {
                    //Legacy comment translated to English.
                    throw new NcfExceptionBase($"用户名不存在或密码不正确：{loginDto.UserName}！");
                }
                SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));
            }

            var userInfo = await GetObjectAsync(z => z.UserName == loginDto.UserName);
            if (userInfo == null)
            {
                throw new NcfExceptionBase($"用户名不存在或密码不正确：{loginDto.UserName}！");
            }
            try
            {
                var adminUserInfo = await TryLoginAsync(userInfo, loginDto.Password, false, loginDto.TenantKey);
                if (adminUserInfo == null)
                {
                    throw new NcfExceptionBase("用户名不存在或密码不正确！");
                }

                string jwtTenantKey = tenantInfo?.TenantKey;
                var tokenResult = await GenerateTokenAsync(adminUserInfo.Id, null, jwtTenantKey);
                token = tokenResult.Token;
                var tokenExpiresUtc = tokenResult.ExpiresUtc;
                var roles = await _serviceProvider.GetService<SysRoleAdminUserInfoService>().GetFullListAsync(o => o.AccountId == adminUserInfo.Id);
                var roleCodes = roles
                    .Select(o => o.RoleCode).Distinct().ToList();
                result.Token = token;
                result.TokenExpiresUtc = tokenExpiresUtc;
                var permissions = await _serviceProvider.GetService<Domain.Services.SysRolePermissionService>().GetFullListAsync(p => roles.Select(o => o.RoleId).Contains(p.RoleId));
                result.MenuTree = await _serviceProvider.GetService<Domain.Services.SysMenuService>().GetAllMenusTreeAsync(false);
                result.UserName = adminUserInfo.UserName;
                result.RoleCodes = roleCodes;
                result.PermissionCodes = permissions.Select(o => o.ResourceCode);
            }
            catch (LoginLockException)
            {
                throw; //Legacy comment translated to English.
            }

            return result;
        }

        /// <summary>
        /// Loads the administrator identity and authorization data used by the current-management view.
        /// </summary>
        /// <param name="id">The administrator identifier.</param>
        /// <returns>The populated account login result.</returns>
        /// <exception cref="NullReferenceException">Thrown when the administrator identifier does not resolve to an entity.</exception>
        public async Task<AccountLoginResultDto> GetAdminUserInfoAsync(int id)
        {
            AccountLoginResultDto result = new AccountLoginResultDto();
            var adminUserInfo = await GetObjectAsync(z => z.Id == id);
            var roles = await _serviceProvider.GetService<SysRoleAdminUserInfoService>().GetFullListAsync(o => o.AccountId == adminUserInfo.Id);
            var roleCodes = roles
                .Select(o => o.RoleCode).Distinct().ToList();
            var permissions = await _serviceProvider.GetService<Domain.Services.SysRolePermissionService>().GetFullListAsync(p => roles.Select(o => o.RoleId).Contains(p.RoleId));
            result.MenuTree = await _serviceProvider.GetService<Domain.Services.SysMenuService>().GetAllMenusTreeAsync(false);
            result.UserName = adminUserInfo.UserName;
            result.RealName = adminUserInfo.RealName;
            result.RoleCodes = roleCodes;
            result.PermissionCodes = permissions.Select(o => o.ResourceCode);
            return result;
        }


        /// <summary>
        /// Generates a JWT for an administrator using the configured role claims and expiration.
        /// </summary>
        /// <param name="memberId">The administrator identifier included in the token.</param>
        /// <param name="expiresUtc">Receives the token expiration instant.</param>
        /// <param name="expiresMinutes">An optional expiration override in minutes.</param>
        /// <returns>The signed JWT text.</returns>
        public string GenerateToken(int memberId, out DateTimeOffset expiresUtc, int? expiresMinutes = null)
        {
            return GenerateToken(memberId, out expiresUtc, expiresMinutes, Array.Empty<string>());
        }

        /// <summary>
        /// Loads the administrator's role codes, creates a signed backend JWT,
        /// and returns the token together with its effective expiration.
        /// </summary>
        /// <param name="memberId">The administrator identifier.</param>
        /// <param name="expiresMinutes">Optional expiration override.</param>
        /// <param name="tenantKey">Optional tenant key included in claims.</param>
        /// <returns>The signed token and expiration instant.</returns>
        public async Task<(string Token, DateTimeOffset ExpiresUtc)> GenerateTokenAsync(int memberId, int? expiresMinutes = null, string tenantKey = null)
        {
            var roleCodes = await GetRoleCodesAsync(memberId);
            var token = GenerateToken(memberId, out var expiresUtc, expiresMinutes, roleCodes, tenantKey);
            return (token, expiresUtc);
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<(string Token, DateTimeOffset ExpiresUtc)> GenerateTokenForDesktopHandoffAsync(
            int memberId,
            DateTimeOffset sourceAuthenticationExpiresUtc)
        {
            var now = DateTimeOffset.UtcNow;
            var configuredExpiresUtc = now.AddMinutes(GetBackendJwtExpireMinutes());
            var expiresUtc = sourceAuthenticationExpiresUtc < configuredExpiresUtc
                ? sourceAuthenticationExpiresUtc
                : configuredExpiresUtc;
            if (expiresUtc <= now.AddSeconds(10))
            {
                throw new NcfExceptionBase("WebView 管理员登录已过期，请重新登录。");
            }

            var roleCodes = await GetRoleCodesAsync(memberId);
            var token = GenerateToken(memberId, expiresUtc, roleCodes);
            return (token, expiresUtc);
        }

        /// <summary>
        /// Loads distinct non-empty role codes for an administrator, returning an
        /// empty list when the role service is unavailable.
        /// </summary>
        /// <param name="memberId">The administrator identifier.</param>
        /// <returns>The distinct role codes.</returns>
        private async Task<List<string>> GetRoleCodesAsync(int memberId)
        {
            var roleService = _serviceProvider.GetService<SysRoleAdminUserInfoService>();
            if (roleService == null)
            {
                return new List<string>();
            }

            var roles = await roleService.GetFullListAsync(o => o.AccountId == memberId);
            return roles
                .Select(o => o.RoleCode)
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Calculates a bounded JWT expiration and delegates token construction.
        /// </summary>
        /// <param name="memberId">The administrator identifier.</param>
        /// <param name="expiresUtc">Receives the calculated expiration.</param>
        /// <param name="expiresMinutes">The optional requested duration.</param>
        /// <param name="roleCodes">The role codes included as claims.</param>
        /// <param name="tenantKey">Optional tenant claim value.</param>
        /// <returns>The signed JWT text.</returns>
        private string GenerateToken(int memberId, out DateTimeOffset expiresUtc, int? expiresMinutes, IEnumerable<string> roleCodes, string tenantKey = null)
        {
            var effectiveExpireMinutes = NormalizeExpireMinutes(expiresMinutes ?? GetBackendJwtExpireMinutes(), AdminAuthConfig.DefaultBackendJwtExpireMinutes);
            expiresUtc = DateTimeOffset.UtcNow.AddMinutes(effectiveExpireMinutes);
            return GenerateToken(memberId, expiresUtc, roleCodes, tenantKey);
        }

        /// <summary>
        /// Builds and signs a backend JWT using configured signing settings and
        /// administrator/role/tenant claims.
        /// </summary>
        /// <param name="memberId">The administrator identifier.</param>
        /// <param name="expiresUtc">The token expiration instant.</param>
        /// <param name="roleCodes">The role codes to encode.</param>
        /// <param name="tenantKey">Optional tenant claim value.</param>
        /// <returns>The serialized signed JWT.</returns>
        private string GenerateToken(int memberId, DateTimeOffset expiresUtc, IEnumerable<string> roleCodes, string tenantKey = null)
        {
            var options = _serviceProvider.GetService<IOptionsSnapshot<JwtSettings>>();
            var jwtSettings = options.Get(JwtSettings.Position_Backend);
            byte[] keyBytes = System.Text.Encoding.ASCII.GetBytes(jwtSettings.SecretKey);
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, memberId.ToString(), ClaimValueTypes.Integer),
                new Claim(NcfAuthorizationPolicyNames.AdminMemberClaim, "1", ClaimValueTypes.String)
            };
            var roleValue = string.Join(",", roleCodes ?? Array.Empty<string>());
            if (!string.IsNullOrWhiteSpace(roleValue))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleValue, ClaimValueTypes.String));
            }
            if (!string.IsNullOrWhiteSpace(tenantKey))
            {
                //Legacy comment translated to English.
                claims.Add(new Claim("TenantKey", tenantKey, ClaimValueTypes.String));
            }

            SecurityTokenDescriptor securityToken = new SecurityTokenDescriptor()
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Audience = jwtSettings.Audience,
                Issuer = jwtSettings.Issuer,
                Expires = expiresUtc.UtcDateTime,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };
            SecurityToken token = tokenHandler.CreateToken(securityToken);
            string tokenStr = tokenHandler.WriteToken(token);
            return tokenStr;
        }

        /// <summary>
        /// Creates a token with the default backend expiration and no explicit role list.
        /// </summary>
        /// <param name="memberId">The administrator identifier.</param>
        /// <returns>The serialized signed JWT.</returns>
        private string GenerateToken(int memberId)
        {
            return GenerateToken(memberId, out _);
        }
    }
}
