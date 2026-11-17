/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SmsRecordService.cs
    File description: SmsRecordService related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.SMS;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xncf.SmsExtension
{
    public class SmsRecordService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IServiceProvider _serviceProvider;
        private const string TokenSessionKey = "SendSMS";
        public SmsRecordService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }
        /// <summary>
        /// set the allowed sending token
        /// </summary>
        /// <returns></returns>
        public string SetSendSmsToken()
        {
            var token = Guid.NewGuid().ToString("n");
            _httpContextAccessor.HttpContext.Session.SetString(TokenSessionKey, token);
            return token;
        }

        /// <summary>
        /// get the SMS sending token
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public string GetSendSmsToken()
        {
            return _httpContextAccessor.HttpContext.Session.GetString(TokenSessionKey);
        }

        public SmsResult SendPhoneCheck(string ip, string phoneNumber, string token,
            bool checkToken = true)
        {
            return SendPhoneCheck(ip, phoneNumber, token, checkToken, "您的注册验证码是：{0}，10分钟内有效。");
        }


        public SmsResult SendPhoneCheck(string ip, string phoneNumber, string token,
            bool checkToken, string format = "")
        {
            if (checkToken)
            {
                var savedToken = _httpContextAccessor.HttpContext.Session.GetString(TokenSessionKey);
                if (savedToken.IsNullOrEmpty() || savedToken.Length != 32 || savedToken != token)
                {
                    return SmsResult.请在60秒后重试;
                }
            }

            //generate and store the verification code
            var phoneCheckCodeCache = _serviceProvider.GetService<PhoneCheckCodeCache>();
            string code = phoneCheckCodeCache.Insert(new PhoneCheckCodeData(phoneNumber), null);

            //send the SMS
            var smsResult = Send(ip, string.Format(format, code), phoneNumber);

            return smsResult;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="content"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="addHotLine"></param>
        /// <param name="postfix"></param>
        /// <returns></returns>
        public SmsResult Send(string ip, string content, string phoneNumber,
            bool addHotLine = true, string postfix = " 客服热线：400-9939-858")
        {
            var senparcSmsSetting = _serviceProvider.GetService<IOptions<SenparcSmsSetting>>();
            ISmsPlatform smsPlatform = null;
            SmsResult smsResult = SmsResult.未知错误;
            content = $"【NCF】{content}{postfix}"; //Legacy comment translated to English.
            smsPlatform = SmsPlatformFactory.GetSmsPlateform(senparcSmsSetting.Value.SmsAccountCORPID, senparcSmsSetting.Value.SmsAccountName, senparcSmsSetting.Value.SmsAccountPassword, senparcSmsSetting.Value.SmsAccountSubNumber);
            smsResult = smsPlatform.Send(content, phoneNumber);
            return smsResult;
        }

        public string GetLastCount()
        {
            var senparcSmsSetting = _serviceProvider.GetService<IOptions<SenparcSmsSetting>>();
            var smsPlatform = SmsPlatformFactory.GetSmsPlateform(senparcSmsSetting.Value.SmsAccountCORPID, senparcSmsSetting.Value.SmsAccountName, senparcSmsSetting.Value.SmsAccountPassword, senparcSmsSetting.Value.SmsAccountSubNumber);
            return smsPlatform.GetLastCount();
        }

        /// <summary>
        /// replace placeholders
        /// </summary>
        /// <param name="content"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public string ReplacePlaceHolder(string content, string name, string sex)
        {
            var smsContent = content.Replace("$姓名$", name.SubString(0, 5)).Replace("$性别$", sex);
            return smsContent;
        }


        /// <summary>
        /// check whether the SMS balance is sufficient
        /// </summary>
        /// <param name="phoneCount"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public int GetSmsUseCount(int phoneCount, string content)
        {
            string finalContent = this.ReplacePlaceHolder(content, "随便五个字", "先生");

            var pieceForEveryPhone = finalContent.Length / 34M;
            var integerpieceForEveryPhone = pieceForEveryPhone % 1 == 0
                ? (int)pieceForEveryPhone
                : (int)(pieceForEveryPhone + 1); //Legacy comment translated to English.
            var totalPiece = phoneCount * integerpieceForEveryPhone;
            return totalPiece;
        }
    }
}