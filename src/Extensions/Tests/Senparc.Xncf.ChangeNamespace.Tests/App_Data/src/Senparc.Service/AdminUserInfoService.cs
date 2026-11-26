using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Senparc.Core.Models;
using Senparc.Repository;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Debug;

namespace Senparc.Service
{
    /// <summary>
    /// Provides administrator account lookup, credential, authentication, and
    /// persistence operations for the sample service layer.
    /// </summary>
    public class AdminUserInfoService : ClientServiceBase<AdminUserInfo>
    {

        private readonly Lazy<IHttpContextAccessor> _contextAccessor;
        /// <summary>
        /// Initializes the administrator-account service.
        /// </summary>
        /// <param name="repository">The administrator account repository.</param>
        /// <param name="httpContextAccessor">Lazy access to the current HTTP context.</param>
        /// <param name="serviceProvider">The base service provider.</param>
        public AdminUserInfoService(AdminUserInfoRepository repository, Lazy<IHttpContextAccessor> httpContextAccessor, IServiceProvider serviceProvider)
            : base(repository, serviceProvider)
        {
            _contextAccessor = httpContextAccessor;
        }

        /// <summary>Checks whether another administrator uses a user name.</summary>
        /// <param name="id">The current account identifier excluded from the check.</param>
        /// <param name="userName">The user name to compare.</param>
        /// <returns><see langword="true"/> when another account uses the name.</returns>
        public bool CheckUserNameExisted(long id, string userName)
        {
            return GetObject(z => z.Id != id && z.UserName.Equals(userName.Trim(), StringComparison.CurrentCultureIgnoreCase)) != null;
        }

        /// <summary>Asynchronously gets an administrator by user name.</summary>
        /// <param name="userName">The administrator user name.</param>
        /// <returns>The matching administrator, or <see langword="null"/>.</returns>
        public async Task<AdminUserInfo> GetUserInfo(string userName)
        {
            var obj = GetObject(z => z.UserName.Equals(userName.Trim()));
            return obj;
        }

        /// <summary>Gets an administrator when the user name and password match.</summary>
        /// <param name="userName">The user name or email identifier.</param>
        /// <param name="password">The plaintext password.</param>
        /// <returns>The authenticated administrator, or <see langword="null"/>.</returns>
        public AdminUserInfo GetUserInfo(string userName, string password)
        {
            AdminUserInfo userInfo = GetObject(z => z.UserName.Equals(userName), null);
            if (userInfo == null)
            {
                return null;
            }
            var codedPassword = GetPassword(password, userInfo.PasswordSalt, false);
            return userInfo.Password == codedPassword ? userInfo : null;
        }

        /// <summary>Hashes a password with the legacy MD5 and salt process.</summary>
        /// <param name="password">The password or pre-hashed value.</param>
        /// <param name="salt">The stored password salt.</param>
        /// <param name="isMD5Password">Whether the input is already MD5-formatted.</param>
        /// <returns>The salted password hash.</returns>
        public string GetPassword(string password, string salt, bool isMD5Password)
        {
            string md5 = password.ToUpper().Replace("-", "");
            if (!isMD5Password)
            {
                md5 = MD5.GetMD5Code(password, "").Replace("-", ""); //originalMD5
            }
            return MD5.GetMD5Code(md5, salt).Replace("-", ""); //encrypt again
        }

        /// <summary>Signs out the current administrator authentication session.</summary>
        public async Task Logout()
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

        /// <summary>Signs in an administrator using the configured admin scheme.</summary>
        /// <param name="userInfo">The administrator account to authenticate.</param>
        /// <param name="rememberMe">Whether the caller requests a persistent session.</param>
        public virtual void Login(AdminUserInfo userInfo, bool rememberMe)
        {
            #region Legacy section

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userInfo.UserName),
                new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString(), ClaimValueTypes.Integer),
                new Claim("AdminMember", "", ClaimValueTypes.String)
            };
            var identity = new ClaimsIdentity(SiteConfig.NcfAdminAuthorizeScheme);
            identity.AddClaims(claims);
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120),
                IsPersistent = false,
            };

            Logout();
            _contextAccessor.Value.HttpContext.SignInAsync(SiteConfig.NcfAdminAuthorizeScheme, new ClaimsPrincipal(identity), authProperties);

            #endregion
        }

        /// <summary>Checks whether an administrator password matches.</summary>
        /// <param name="userName">The administrator user name.</param>
        /// <param name="password">The plaintext password.</param>
        /// <returns><see langword="true"/> when the password matches.</returns>
        public bool CheckPassword(string userName, string password)
        {
            var userInfo = GetObject(z => z.UserName.Equals(userName));
            if (userInfo == null)
            {
                return false;
            }
            var codedPassword = GetPassword(password, userInfo.PasswordSalt, false);
            return userInfo.Password == codedPassword;
        }

        /// <summary>Authenticates an administrator and starts a session on success.</summary>
        /// <param name="userNameOrEmail">The user name or email identifier.</param>
        /// <param name="password">The plaintext password.</param>
        /// <param name="rememberMe">Whether the caller requests a persistent session.</param>
        /// <returns>The authenticated administrator, or <see langword="null"/>.</returns>
        public AdminUserInfo TryLogin(string userNameOrEmail, string password, bool rememberMe)
        {
            AdminUserInfo userInfo = GetUserInfo(userNameOrEmail, password);
            if (userInfo != null)
            {
                Login(userInfo, rememberMe);
                return userInfo;
            }
            else
            {
                return null;
            }
        }

        /// <summary>Creates and persists an administrator account.</summary>
        /// <param name="objDto">The account creation fields.</param>
        public void CreateAdminUserInfo(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            string userName = objDto.UserName;
            string password = objDto.Password;
            var obj = new AdminUserInfo(ref userName, ref password, null, null, objDto.Note);
            SaveObject(obj);
        }

        /// <summary>Updates an existing administrator account.</summary>
        /// <param name="objDto">The account update fields.</param>
        /// <exception cref="Exception">Thrown when the account does not exist.</exception>
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

        /// <summary>Gets one administrator DTO with optional related includes.</summary>
        /// <param name="id">The administrator identifier.</param>
        /// <param name="includes">Optional related-navigation paths.</param>
        /// <returns>The mapped administrator DTO.</returns>
        public CreateOrUpdate_AdminUserInfoDto GetAdminUserInfo(int id,params string[] includes)
        {
            var obj = GetObject(z => z.Id == id, includes: includes);

            var objDto = base.Mapper.Map<CreateOrUpdate_AdminUserInfoDto>(obj);

            return objDto;
        }

        /// <summary>Gets administrators by a set of identifiers.</summary>
        /// <param name="ids">The administrator identifiers.</param>
        /// <param name="includes">Optional related-navigation paths.</param>
        /// <returns>The matching administrators ordered by identifier.</returns>
        public List<AdminUserInfo> GetAdminUserInfo(List<int> ids,params string[] includes)
        {
            return GetFullList(z => ids.Contains(z.Id), z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, includes: includes);
        }

        /// <summary>
        /// Creates the initial administrator account only when no account exists.
        /// </summary>
        /// <param name="userName">Receives the generated initial user name.</param>
        /// <param name="password">Receives the generated initial password.</param>
        /// <returns>The initial account, or <see langword="null"/> when initialization already occurred.</returns>
        public AdminUserInfo Init(out string userName, out string password)
        {
            userName = null;
            password = null;

            var oldAdminUserInfo = GetObject(z => true);
            if (oldAdminUserInfo != null)
            {
                return null;
            }
            var adminUserInfo = new AdminUserInfo(ref userName, ref password, null, null, "初始化数据");
            SaveObject(adminUserInfo);
            return adminUserInfo;
        }

        /// <summary>Deletes an administrator by identifier.</summary>
        /// <param name="id">The administrator identifier.</param>
        /// <exception cref="Exception">Thrown when the account does not exist.</exception>
        public void DeleteObject(int id)
        {
            var obj = GetObject(z => z.Id == id);
            if (obj == null)
            {
                throw new Exception("用户信息不存在！");
            }
            DeleteObject(obj);
        }

        /// <inheritdoc />
        public override void SaveObject(AdminUserInfo obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("AdminUserInfo{2}：{0}（ID：{1}）", obj.UserName, obj.Id, isInsert ? "新增" : "编辑");
        }

        /// <inheritdoc />
        public override void DeleteObject(AdminUserInfo obj)
        {
            base.DeleteObject(obj);
            LogUtility.WebLogger.InfoFormat("AdminUserInfo被删除：{0}（ID：{1}）", obj.UserName, obj.Id);
        }
    }
}
