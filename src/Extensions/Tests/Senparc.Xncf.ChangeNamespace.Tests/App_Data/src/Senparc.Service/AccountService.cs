/* Note:

 * Legacy comment translated to English.

 * Legacy comment translated to English.

 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.HttpUtility;
using Senparc.Core.Cache;
using Senparc.Core.Models;
using Senparc.Repository;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Senparc.Ncf.Service;
using Senparc.Service.OperationQueue;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Service
{
    /// <summary>
    /// Provides account lookup, credential, profile, login, and Weixin-account
    /// synchronization operations for the sample service layer.
    /// </summary>
    public class AccountService : ClientServiceBase<Account> /*, UserService*/
    {
        private readonly Lazy<IHttpContextAccessor> _httpContextAccessor;
        private string GetSalt => DateTime.Now.Ticks.ToString();

        /// <summary>
        /// Initializes the account service.
        /// </summary>
        /// <param name="accountRepo">The account repository.</param>
        /// <param name="httpContextAccessor">Lazy access to the current HTTP context.</param>
        /// <param name="serviceProvider">The service provider used by the base service.</param>
        public AccountService(AccountRepository accountRepo, Lazy<IHttpContextAccessor> httpContextAccessor, IServiceProvider serviceProvider)
            : base(accountRepo, serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>Checks whether another account already uses a user name.</summary>
        /// <param name="id">The current account identifier excluded from the check.</param>
        /// <param name="userName">The user name to compare case-insensitively.</param>
        /// <returns><see langword="true"/> when another account uses the name.</returns>
        public bool CheckUserNameExisted(long id, string userName)
        {
            userName = userName.Trim();

            return
            this.GetObject(
                z => z.Id != id && z.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase)) != null;
        }

        /// <summary>Checks whether credentials resolve to an account.</summary>
        /// <param name="userName">The user name, email, or phone identifier.</param>
        /// <param name="password">The plaintext password to verify.</param>
        /// <returns><see langword="true"/> when the credentials match.</returns>
        public bool CheckPassword(string userName, string password)
        {
            var user = GetAccount(userName, password);
            return user != null;
        }

        /// <summary>Changes an account password and persists a new password salt.</summary>
        /// <param name="id">The account identifier.</param>
        /// <param name="password">The new plaintext password.</param>
        /// <returns><see langword="true"/> after persistence completes.</returns>
        public bool ChangePassword(int id, string password)
        {
            var user = GetObject(z => z.Id == id);
            var salt = GetSalt;
            user.Password = GetPassword(password, salt);
            user.PasswordSalt = salt;
            SaveObject(user);
            return true;
        }
        /// <summary>Updates supplied basic account profile fields.</summary>
        /// <param name="id">The account identifier.</param>
        /// <param name="realName">The replacement real name, or null to preserve it.</param>
        /// <param name="email">The replacement email, or null to preserve it.</param>
        /// <param name="headImg">The replacement avatar path, or null to preserve it.</param>
        /// <returns><see langword="true"/> after persistence completes.</returns>
        public bool ChangeBasic(int id, string realName, string email, string headImg = null)
        {
            var user = GetObject(z => z.Id == id);
            user.RealName = realName ?? user.RealName;
            user.Email = email ?? user.Email;
            user.PicUrl = headImg ?? user.PicUrl;
            SaveObject(user);
            return true;
        }

        /// <summary>Checks whether another account already uses a phone number.</summary>
        /// <param name="id">The current account identifier excluded from the check.</param>
        /// <param name="phone">The phone number to compare.</param>
        /// <returns><see langword="true"/> when another account uses the number.</returns>
        public bool CheckPhoneExisted(long id, string phone)
        {
            phone = phone.Trim();
            return
            this.GetObject(
                z => z.Id != id && phone.Equals(z.Phone, StringComparison.CurrentCultureIgnoreCase)) != null;
        }
        /// <summary>Checks whether another account already uses an email address.</summary>
        /// <param name="id">The current account identifier excluded from the check.</param>
        /// <param name="email">The email address to compare.</param>
        /// <returns><see langword="true"/> when another account uses the address.</returns>
        public bool CheckEmailExisted(long id, string email)
        {
            email = email.Trim();
            return
            this.GetObject(
                z => z.Id != id && email.Equals(z.Email, StringComparison.CurrentCultureIgnoreCase)) != null;
        }

        /// <summary>Gets the account represented by a full-account object.</summary>
        /// <param name="fullUser">The source account projection.</param>
        /// <returns>The matching account, or <see langword="null"/> when the input is null.</returns>
        public Account GetAccount(FullAccount fullUser)
        {
            if (fullUser == null)
            {
                return null;
            }
            return GetAccount(fullUser.UserName);
        }

        /// <summary>Gets an account by user name using case-insensitive comparison.</summary>
        /// <param name="userName">The account user name.</param>
        /// <returns>The matching account, or <see langword="null"/>.</returns>
        public Account GetAccount(string userName)
        {
            userName = userName.Trim();
            return this.GetObject(z => z.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>Hashes a password using the legacy two-stage MD5 and salt scheme.</summary>
        /// <param name="password">The password or pre-hashed password.</param>
        /// <param name="salt">The account salt.</param>
        /// <param name="isMD5Password">Whether the input is already an MD5-style value.</param>
        /// <returns>The normalized salted password hash.</returns>
        public string GetPassword(string password, string salt, bool isMD5Password = true)
        {
            string md5 = password.ToUpper().Replace("-", "");
            if (!isMD5Password)
            {
                md5 = MD5.GetMD5Code(password, "").Replace("-", ""); //originalMD5
            }
            return MD5.GetMD5Code(md5, salt).Replace("-", ""); //encrypt again
        }

        /// <summary>Signs out the current HTTP user and clears the account cache session.</summary>
        public virtual void Logout()
        {
            try
            {
                var fullAccountCache = _serviceProvider.GetService<FullAccountCache>();
                fullAccountCache.ForceLogout(_httpContextAccessor.Value.HttpContext.User.Identity.Name);

                _httpContextAccessor.Value.HttpContext.SignOutAsync(
                    UserAuthorizeAttribute.AuthenticationScheme);

                //Legacy comment translated to English.
                //Legacy comment translated to English.
            }
            catch (Exception ex)
            {
                Ncf.Log.LogUtility.Account.Error("退出登录失败。", ex);
            }
        }

        /// <summary>
        /// Signs in a user through the configured authentication scheme and optionally
        /// records the login timestamp.
        /// </summary>
        /// <param name="userName">The account user name.</param>
        /// <param name="rememberMe">Whether the caller requests a persistent login.</param>
        /// <param name="roles">Optional role values retained for compatibility.</param>
        /// <param name="recordLoginInfo">Whether login information should be recorded.</param>
        public virtual void Login(string userName, bool rememberMe, IEnumerable<string> roles, bool recordLoginInfo)
        {
            try
            {
                var httpContext = _httpContextAccessor.Value.HttpContext;

                #region Legacy section

                var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, userName),
                            //new Claim(ClaimTypes.Role, string.Join(",",roles)),
                            new Claim("UserMember", "", ClaimValueTypes.String)
                        };

                //var claimsIdentity = new ClaimsIdentity(claims,
                //    UserAuthorizeAttribute.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                var identity = new ClaimsIdentity(UserAuthorizeAttribute.AuthenticationScheme);
                identity.AddClaims(claims);
                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = false,
                    // Refreshing the authentication session should be allowed.

                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120),
                    // The time at which the authentication ticket expires. A 
                    // value set here overrides the ExpireTimeSpan option of 
                    // CookieAuthenticationOptions set with AddCookie.

                    IsPersistent = false,
                    // Whether the authentication session is persisted across 
                    // multiple requests. Required when setting the 
                    // ExpireTimeSpan option of CookieAuthenticationOptions 
                    // set with AddCookie. Also required when setting 
                    // ExpiresUtc.

                    //IssuedUtc = <DateTimeOffset>,
                    // The time at which the authentication ticket was issued.

                    //RedirectUri = <string>
                    // The full path or absolute URI to be used as an http 
                    // redirect response value.
                };

                Logout();//Legacy comment translated to English.
                httpContext.SignInAsync(UserAuthorizeAttribute.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

                #endregion

                //Legacy comment translated to English.

                using (var wrap = this.InstanceAutoDetectChangeContextWrap())
                {
                    var account = this.GetAccount(userName);
                    if (account != null)
                    {
                        account.ThisLoginTime = DateTime.Now;
                        this.SaveObject(account); //Legacy comment translated to English.
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(userName + ":" + ex.Message, ex);
            }
        }

        /// <summary>Creates and persists a new account with the supplied credentials and profile values.</summary>
        /// <param name="userName">The account user name.</param>
        /// <param name="email">The account email.</param>
        /// <param name="phone">The account phone number.</param>
        /// <param name="password">The initial password.</param>
        /// <param name="openId">The Weixin open identifier.</param>
        /// <param name="weixinUnionId">The Weixin union identifier.</param>
        /// <param name="nickname">The display nickname.</param>
        /// <returns>The persisted account.</returns>
        public Account CreateAccount(string userName, string email, string phone, string password, string openId, string weixinUnionId, string nickname)
        {
            var salt = GetSalt;
            var model = new Account()
            {
                UserName = userName,
                NickName = nickname ?? "",
                Phone = phone,
                PasswordSalt = salt,
                Email = email,
                WeixinOpenId = openId,
                Password = GetPassword(password, salt, true),
                AddTime = DateTime.Now,
                Sex = (int)Sex.未设置,
                ThisLoginTime = DateTime.Now,
                LastLoginTime = DateTime.Now,
                Balance = 0,
                Package = 0,
                WeixinUnionId = weixinUnionId,
                Flag = false,
            };

            this.SaveObject(model);
            return model;
        }

        /// <summary>Generates a unique sample user name with the <c>NCF_</c> prefix.</summary>
        /// <returns>A user name not currently used by an account.</returns>
        public string GetNewUserName()
        {
            string userName;
            Account account;

            do
            {
                userName = $"NCF_{Guid.NewGuid().ToString("n").Substring(0, 8)}";
                account = this.GetAccount(userName);
            } while (account != null);

            return userName;
        }

        /// <summary>Authenticates by user name, email, or phone and starts a login session.</summary>
        /// <param name="userNameOrEmailOrPhone">The account identifier.</param>
        /// <param name="password">The plaintext password.</param>
        /// <param name="rememberMe">Whether the login should be remembered.</param>
        /// <param name="recordLoginInfo">Whether login information should be recorded.</param>
        /// <returns>The authenticated account, or <see langword="null"/>.</returns>
        public Account TryLogin(string userNameOrEmailOrPhone, string password, bool rememberMe, bool recordLoginInfo)
        {
            var user = this.GetAccount(userNameOrEmailOrPhone, password);
            if (user != null)
            {
                this.Login(user.UserName, rememberMe, null, recordLoginInfo);
                return user;
            }
            else
            {
                return null;
            }
        }

        /// <summary>Gets an account when the identifier and password hash match.</summary>
        /// <param name="userName">A user name, email, or phone identifier.</param>
        /// <param name="password">The plaintext password to verify.</param>
        /// <returns>The matching account, or <see langword="null"/>.</returns>
        public Account GetAccount(string userName, string password)
        {
            userName = userName.Trim();
            Account account =
                this.GetObject(z => userName.Equals(z.UserName, StringComparison.CurrentCultureIgnoreCase)
                    || userName.Equals(z.Email, StringComparison.CurrentCultureIgnoreCase)
                    || userName.Equals(z.Phone, StringComparison.CurrentCultureIgnoreCase));
            if (account == null)
            {
                return null;
            }
            var codedPassword = this.GetPassword(password, account.PasswordSalt, true);
            return account.Password == codedPassword ? account : null;
        }

        /// <summary>
        /// Creates or updates an account from Weixin user information and queues
        /// avatar synchronization.
        /// </summary>
        /// <param name="userInfo">The Weixin user profile and optional P2P account data.</param>
        /// <param name="account">An existing account to update, or null to resolve/create one.</param>
        /// <returns>The synchronized account.</returns>
        public Account CreateOrUpdateBySenparcWeixin(UserInfoJson userInfo, Account account = null)
        {
            var openid = userInfo.openid;
            var unionid = userInfo.unionid;
            var nickname = userInfo.nickname;
            var headimgurl = userInfo.headimgurl;

            var fullAccount = (userInfo.P2PData is string p2pData && !p2pData.IsNullOrEmpty())
                                ? JsonConvert.DeserializeObject<FullAccount>(p2pData)
                                : null;

            if (fullAccount == null)
            {
                LogUtility.Account.Error($"userInfo.P2PData 中的 P2PData可能为Null,userInfo：{userInfo?.ToJson()}");
            }

            if (fullAccount.UserName == "zhensherlock")//Legacy comment translated to English.
            {
                account = GetObject(z => z.UserName == "zhensherlock");

                account.WeixinUnionId = userInfo.unionid;//updateUnionId
                account.WeixinOpenId = userInfo.openid;//updateOpenId
                SaveObject(account);
            }

            account = account ?? GetObject(z => z.WeixinUnionId != null && z.WeixinUnionId == unionid);
            if (account == null)
            {
                string userName = fullAccount.UserName ?? GetNewUserName();
                var email = fullAccount.Email ?? "";
                var phone = fullAccount.Phone ?? "";

                account = CreateAccount(userName, email, phone, "", openid, unionid, nickname);
            }

            if (!string.IsNullOrWhiteSpace(fullAccount?.UserName))
            {
                account.EmailChecked = fullAccount.EmailChecked;
                account.PhoneChecked = fullAccount.PhoneChecked;
            }

            account.WeixinUnionId = unionid;

            var defaultHeadimgUrl = $"{SiteConfig.DomainName}/images/user/avatar/default.png";

            if (headimgurl.IsNullOrEmpty())
            {
                account.HeadImgUrl = defaultHeadimgUrl;
                account.PicUrl = defaultHeadimgUrl;
            }
            else if (account.HeadImgUrl != headimgurl)
            {
                account.HeadImgUrl = headimgurl;

                var fileName = $@"/Upload/Account/headimgurl.{DateTime.Now.Ticks + Guid.NewGuid().ToString("n").Substring(0, 8)}.jpg";

                DownLoadPic(userInfo.headimgurl, fileName);

                account.PicUrl = fileName;
            }
            SaveObject(account);
            return account;
        }

        /// <summary>Downloads an avatar image to the web application's upload path.</summary>
        /// <param name="picUrl">The remote image URL.</param>
        /// <param name="fileName">The application-relative destination path.</param>
        /// <exception cref="IOException">Thrown when the destination file cannot be created.</exception>
        private void DownLoadPic(string picUrl, string fileName)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Get.Download(picUrl, stream);

                using (var fs = new FileStream(Server.GetWebMapPath("~" + fileName), FileMode.CreateNew))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fs);
                    fs.Flush();
                }
            }
        }

        /// <summary>Creates an account from OAuth user data and queues avatar synchronization.</summary>
        /// <param name="userInfo">The OAuth user profile.</param>
        /// <returns>The newly created account.</returns>
        public Account CreateAccountByUserInfo(OAuthUserInfo userInfo)
        {
            using (var wrap = this.InstanceAutoDetectChangeContextWrap())
            {
                Account account = null;
                LogUtility.SystemLogger.Debug($"开始创建用户（Account），OAuthUserInfo：\r\n{userInfo.ToJson()}");

                var nickname = userInfo.nickname;
                string userName = GetNewUserName();
                account = CreateAccount(userName, "", "", "", userInfo.openid, "", nickname);

                var url = userInfo.headimgurl.Replace("Http", "Https");
                account.HeadImgUrl = url;
                account.NickName = nickname;
                account.Sex = (byte)userInfo.sex;
                account.PicUrl = url;
                SaveObject(account);

                LogUtility.SystemLogger.Debug($"进行异步头像更新：{userInfo.headimgurl}");

                var operationQueue = new OperationQueue.OperationQueue();
                operationQueue.Add($"{account.Id}-{DateTime.Now.Ticks.ToString()}", OperationQueueType.更新用户头像, new List<object>() { account.Id, userInfo.headimgurl });

                return account;
            }
        }

        /// <summary>Creates or updates an account using OAuth user identity data.</summary>
        /// <param name="userInfo">The OAuth user profile.</param>
        /// <returns>The created or updated account.</returns>
        public Account CreateOrUpdateByUserInfo(OAuthUserInfo userInfo)
        {
            var account = GetObject(z => z.WeixinOpenId == userInfo.openid) ??
                              CreateAccount(GetNewUserName(), "", "", "", userInfo.openid, "", userInfo.nickname);
            LogUtility.SystemLogger.Debug($"开始创建用户（Account），OAuthUserInfo：\r\n{userInfo.ToJson()}");
            var url = userInfo.headimgurl.Replace("Http", "Https");
            account.HeadImgUrl = url;
            account.NickName = userInfo.nickname;
            account.Sex = (byte)userInfo.sex;
            account.PicUrl = url;
            SaveObject(account);

            LogUtility.SystemLogger.Debug($"进行异步头像更新：{userInfo.headimgurl}");

            var operationQueue = new OperationQueue.OperationQueue();
            operationQueue.Add($"{account.Id}-{DateTime.Now.Ticks.ToString()}", OperationQueueType.更新用户头像, new List<object>() { account.Id, userInfo.headimgurl });

            return account;
        }

        /// <summary>Updates an existing account's OAuth profile and removes its old local avatar.</summary>
        /// <param name="userInfo">The replacement OAuth profile.</param>
        /// <param name="account">The account to update.</param>
        public void UpdateAccountByUserInfo(OAuthUserInfo userInfo, Account account)
        {
            if (!account.PicUrl.IsNullOrEmpty())
            {
                File.Delete(Server.GetMapPath("~" + account.PicUrl));
            }
            account.HeadImgUrl = userInfo.headimgurl;
            account.NickName = userInfo.nickname;
            SaveObject(account);
        }

        /// <inheritdoc />
        public override void SaveObject(Account obj)
        {
            var isInsert = base.IsInsert(obj);
            if (isInsert)
            {
                obj.Flag = false;
            }
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("User{2}：{0}（ID：{1}）", obj.UserName, obj.Id, isInsert ? "新增" : "编辑");

            //clear the cache
            var fullUserCache = _serviceProvider.GetService<FullAccountCache>();
            using (fullUserCache.Cache.BeginCacheLock(FullAccountCache.CACHE_KEY, obj.Id.ToString()))
            {
                fullUserCache.RemoveObject(obj.UserName);
            }
        }

        /// <inheritdoc />
        public override void DeleteObject(Account obj)
        {
            obj.Flag = true;
            base.SaveObject(obj);
            LogUtility.WebLogger.Info($"User被删除：{obj.UserName}（ID：{obj.Id}）");

            //clear the cache
            var fullUserCache = _serviceProvider.GetService<FullAccountCache>();
            fullUserCache.RemoveObject(obj.UserName);
        }
    }
}
