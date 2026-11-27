/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SmsRecordService.cs
    File description: SmsRecordService related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.12.16.2 Improve source documentation, headers, and module metadata consistency.

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
        /// <summary>
        /// Initializes the SMS record service with HTTP session access and dependency resolution.
        /// </summary>
        /// <param name="httpContextAccessor">The accessor used for SMS session tokens.</param>
        /// <param name="serviceProvider">The provider used to resolve SMS caches and settings.</param>
        public SmsRecordService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }
        /// <summary>
        /// Creates and stores a session token required by protected SMS-send operations.
        /// </summary>
        /// <returns>The newly generated session token.</returns>
        public string SetSendSmsToken()
        {
            var token = Guid.NewGuid().ToString("n");
            _httpContextAccessor.HttpContext.Session.SetString(TokenSessionKey, token);
            return token;
        }

        /// <summary>
        /// Reads the SMS-send token stored in the current HTTP session.
        /// </summary>
        /// <returns>The session token, or <see langword="null"/> when none is stored.</returns>
        public string GetSendSmsToken()
        {
            return _httpContextAccessor.HttpContext.Session.GetString(TokenSessionKey);
        }

        /// <summary>
        /// Creates a phone verification code, stores it in the verification-code cache,
        /// and sends it using the default registration message template.
        /// </summary>
        /// <param name="ip">The client IP address used by the SMS provider.</param>
        /// <param name="phoneNumber">The destination phone number.</param>
        /// <param name="token">The session token to validate.</param>
        /// <param name="checkToken">Whether the session token must match before sending.</param>
        /// <returns>The provider result.</returns>
        public SmsResult SendPhoneCheck(string ip, string phoneNumber, string token,
            bool checkToken = true)
        {
            return SendPhoneCheck(ip, phoneNumber, token, checkToken, "您的注册验证码是：{0}，10分钟内有效。");
        }


        /// <summary>
        /// Creates a phone verification code, stores it in the cache, and sends a formatted SMS.
        /// </summary>
        /// <param name="ip">The client IP address.</param>
        /// <param name="phoneNumber">The destination phone number.</param>
        /// <param name="token">The session token to validate.</param>
        /// <param name="checkToken">Whether to validate the session token.</param>
        /// <param name="format">The composite format string receiving the generated code.</param>
        /// <returns>The provider result.</returns>
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
        /// Sends a message through the configured SMS platform after applying the NCF prefix and suffix.
        /// </summary>
        /// <param name="ip">The client IP address.</param>
        /// <param name="content">The message body.</param>
        /// <param name="phoneNumber">The destination phone number.</param>
        /// <param name="addHotLine">Legacy flag retained for compatibility.</param>
        /// <param name="postfix">The suffix appended to the message.</param>
        /// <returns>The provider result.</returns>
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

        /// <summary>
        /// Gets the last SMS balance/count reported by the configured provider.
        /// </summary>
        /// <returns>The provider-reported count text.</returns>
        public string GetLastCount()
        {
            var senparcSmsSetting = _serviceProvider.GetService<IOptions<SenparcSmsSetting>>();
            var smsPlatform = SmsPlatformFactory.GetSmsPlateform(senparcSmsSetting.Value.SmsAccountCORPID, senparcSmsSetting.Value.SmsAccountName, senparcSmsSetting.Value.SmsAccountPassword, senparcSmsSetting.Value.SmsAccountSubNumber);
            return smsPlatform.GetLastCount();
        }

        /// <summary>
        /// Replaces the supported name and gender placeholders in an SMS template.
        /// </summary>
        /// <param name="content">The template containing placeholders.</param>
        /// <param name="name">The recipient name.</param>
        /// <param name="sex">The recipient gender text.</param>
        /// <returns>The rendered SMS content.</returns>
        public string ReplacePlaceHolder(string content, string name, string sex)
        {
            var smsContent = content.Replace("$姓名$", name.SubString(0, 5)).Replace("$性别$", sex);
            return smsContent;
        }


        /// <summary>
        /// Estimates the number of provider message segments required for a message batch.
        /// </summary>
        /// <param name="phoneCount">The number of recipients.</param>
        /// <param name="content">The message template used for length estimation.</param>
        /// <returns>The estimated total segment count.</returns>
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
