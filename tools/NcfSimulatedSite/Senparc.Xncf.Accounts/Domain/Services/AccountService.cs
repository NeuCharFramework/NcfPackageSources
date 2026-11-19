/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AccountService.cs
    File description: AccountService related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20261119
    Change description: v0.3.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

/* Note:

 * Legacy comment translated to English.

 * Legacy comment translated to English.

 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;
using Senparc.Ncf.Service;
using Senparc.Service.ACL;
using Senparc.Xncf.Accounts.Domain.Cache;
using Senparc.Xncf.Accounts.Domain.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
//using System.IdentityModel.Tokens.Jwt;
//using Microsoft.IdentityModel.Tokens;

namespace Senparc.Xncf.Accounts.Domain.Services
{
    public class AccountService : BaseClientService<Account> /*, UserService*/
    {
        private readonly Lazy<IHttpContextAccessor> _httpContextAccessor;
        private string GetSalt => DateTime.Now.Ticks.ToString();

        public AccountService(AccountRepository accountRepo, Lazy<IHttpContextAccessor> httpContextAccessor, IServiceProvider serviceProvider)
            : base(accountRepo, serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool CheckUserNameExisted(long id, string userName)
        {
            userName = userName.Trim().ToUpper();

            return this.GetObject(
                z => z.Id != id && z.UserName.ToUpper() == userName /*z.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase)*/) != null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool CheckPassword(string userName, string password)
        {
            var user = GetAccount(userName, password);
            return user != null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool ChangePassword(int id, string password)
        {
            var user = GetObject(z => z.Id == id);
            var salt = GetSalt;
            user.Password = GetPassword(password, salt);
            user.PasswordSalt = salt;
            SaveObject(user);
            return true;
        }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="realName"></param>
        /// <param name="email"></param>
        /// <param name="headImg"></param>
        /// <returns></returns>
        public bool ChangeBasic(int id, string realName, string email, string headImg = null)
        {
            var user = GetObject(z => z.Id == id);
            user.RealName = realName ?? user.RealName;
            user.Email = email ?? user.Email;
            user.PicUrl = headImg ?? user.PicUrl;
            SaveObject(user);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="phone"></param>
        /// <returns></returns>
        public bool CheckPhoneExisted(long id, string phone)
        {
            phone = phone.Trim();
            return
            this.GetObject(
                z => z.Id != id && phone.Equals(z.Phone, StringComparison.CurrentCultureIgnoreCase)) != null;
        }
        public bool CheckEmailExisted(long id, string email)
        {
            email = email.Trim();
            return
            this.GetObject(
                z => z.Id != id && email.Equals(z.Email, StringComparison.CurrentCultureIgnoreCase)) != null;
        }

        public Account GetAccount(FullAccount fullUser)
        {
            if (fullUser == null)
            {
                return null;
            }
            return GetAccount(fullUser.UserName);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        [ApiBind]
        public Account GetAccount(string userName)
        {
            userName = userName.Trim();
            return this.GetObject(z => z.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase));
        }

        public string GetPassword(string password, string salt, bool isMD5Password = true)
        {
            string md5 = password.ToUpper().Replace("-", "");
            if (!isMD5Password)
            {
                md5 = MD5.GetMD5Code(password, "").Replace("-", ""); //originalMD5
            }
            return MD5.GetMD5Code(md5, salt).Replace("-", ""); //encrypt again
        }

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
                Sex = (int)Sex.Unset,
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

        /// <summary>
        /// automaticgeneratenewuser name
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="userNameOrEmailOrPhone"></param>
        /// <param name="password"></param>
        /// <param name="rememberMe"></param>
        /// <param name="recordLoginInfo"></param>
        /// <returns></returns>
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

        /* Legacy comment translated to English.



        /// <summary>

        Legacy comment translated to English.

        /// </summary>

        /// <param name="userInfo"></param>

        /// <param name="account"></param>

        /// <returns></returns>

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

                Legacy comment translated to English.

            }



            Legacy comment translated to English.

            {

                account = GetObject(z => z.UserName == "JeffreySu");



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



                Legacy comment translated to English.

                DownLoadPic(_serviceProvider,userInfo.headimgurl, fileName);



                account.PicUrl = fileName;

            }

            SaveObject(account);

            return account;

        }



        /// <summary>

        Legacy comment translated to English.

        /// </summary>

        /// <param name="picUrl"></param>

        /// <param name="fileName"></param>

        private void DownLoadPic(IServiceProvider serviceProvider, string picUrl, string fileName)

        {

            using (MemoryStream stream = new MemoryStream())

            {

                Get.Download(serviceProvider,picUrl, stream);



                using (var fs = new FileStream(Server.GetWebMapPath("~" + fileName), FileMode.CreateNew))

                {

                    stream.Seek(0, SeekOrigin.Begin);

                    stream.CopyTo(fs);

                    fs.Flush();

                }

            }

        }



        public Account CreateAccountByUserInfo(OAuthUserInfo userInfo)

        {

            using (var wrap = this.InstanceAutoDetectChangeContextWrap())

            {

                Account account = null;

                LogUtility.SystemLogger.Debug($"startcreateuser（Account），OAuthUserInfo：\r\n{userInfo.ToJson()}");



                var nickname = userInfo.nickname;

                string userName = GetNewUserName();

                account = CreateAccount(userName, "", "", "", userInfo.openid, "", nickname);



                var url = userInfo.headimgurl.Replace("Http", "Https");

                account.HeadImgUrl = url;

                account.NickName = nickname;

                account.Sex = (byte)userInfo.sex;

                Legacy comment translated to English.

                SaveObject(account);



                Legacy comment translated to English.



                Legacy comment translated to English.

                var operationQueue = new OperationQueue.OperationQueue();

                Legacy comment translated to English.



                return account;

            }

        }



        public Account CreateOrUpdateByUserInfo(OAuthUserInfo userInfo)

        {

            var account = GetObject(z => z.WeixinOpenId == userInfo.openid) ??

                              CreateAccount(GetNewUserName(), "", "", "", userInfo.openid, "", userInfo.nickname);

            LogUtility.SystemLogger.Debug($"startcreateuser（Account），OAuthUserInfo：\r\n{userInfo.ToJson()}");

            var url = userInfo.headimgurl.Replace("Http", "Https");

            account.HeadImgUrl = url;

            account.NickName = userInfo.nickname;

            account.Sex = (byte)userInfo.sex;

            Legacy comment translated to English.

            SaveObject(account);



            Legacy comment translated to English.



            Legacy comment translated to English.

            var operationQueue = new OperationQueue.OperationQueue();

            Legacy comment translated to English.



            return account;

        }



        public void UpdateAccountByUserInfo(OAuthUserInfo userInfo, Account account)

        {

            Legacy comment translated to English.

            if (!account.PicUrl.IsNullOrEmpty())

            {

                File.Delete(Server.GetMapPath("~" + account.PicUrl));

            }

            account.HeadImgUrl = userInfo.headimgurl;

            account.NickName = userInfo.nickname;

            SaveObject(account);

        }



        */

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
            //Legacy comment translated to English.
            using (fullUserCache.Cache.BeginCacheLock(FullAccountCache.CACHE_KEY, obj.Id.ToString()))
            {
                fullUserCache.RemoveObject(obj.UserName);
            }
        }

        //TODO:provideasynchronous methods

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
