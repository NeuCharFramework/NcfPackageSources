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

namespace Senparc.Service
{
    /// <summary>
    /// Coordinates SMS verification tokens, provider dispatch, and usage estimates.
    /// </summary>
    public class SmsRecordService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IServiceProvider _serviceProvider;
        private const string TokenSessionKey = "SendSMS";
        /// <summary>
        /// Initializes the SMS record service.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor for the current session.</param>
        /// <param name="serviceProvider">Provider for cache and SMS dependencies.</param>
        public SmsRecordService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = _serviceProvider;
        }
        /// <summary>
        /// Creates and stores a new session token used to authorize SMS sending.
        /// </summary>
        /// <returns>The newly generated session token.</returns>
        public string SetSendSmsToken()
        {
            var token = Guid.NewGuid().ToString("n");
            _httpContextAccessor.HttpContext.Session.SetString(TokenSessionKey, token);
            return token;
        }

        /// <summary>
        /// Gets the session token currently authorizing SMS sending.
        /// </summary>
        /// <returns>The stored token, or <see langword="null"/> when absent.</returns>
        public string GetSendSmsToken()
        {
            return _httpContextAccessor.HttpContext.Session.GetString(TokenSessionKey);
        }

        /// <summary>Sends a phone verification code using the default message format.</summary>
        /// <param name="ip">The caller IP address used for provider accounting.</param>
        /// <param name="phoneNumber">The destination phone number.</param>
        /// <param name="token">The session SMS authorization token.</param>
        /// <param name="checkToken">Whether the session token must be validated.</param>
        /// <returns>The provider send result.</returns>
        public SmsResult SendPhoneCheck(string ip, string phoneNumber, string token,
            bool checkToken = true)
        {
            return SendPhoneCheck(ip, phoneNumber, token, checkToken, "您的注册验证码是：{0}，10分钟内有效。");
        }


        /// <summary>Generates, caches, and sends a phone verification code.</summary>
        /// <param name="ip">The caller IP address used for provider accounting.</param>
        /// <param name="phoneNumber">The destination phone number.</param>
        /// <param name="token">The session SMS authorization token.</param>
        /// <param name="checkToken">Whether the session token must be validated.</param>
        /// <param name="format">The message format receiving the generated code.</param>
        /// <returns>The provider send result.</returns>
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
        /// Sends a message through the configured SMS provider after adding the NCF
        /// prefix and optional hotline suffix.
        /// </summary>
        /// <param name="ip">The caller IP address retained for compatibility.</param>
        /// <param name="content">The message body.</param>
        /// <param name="phoneNumber">The destination phone number.</param>
        /// <param name="addHotLine">Whether the hotline option should be considered.</param>
        /// <param name="postfix">The suffix appended to the message.</param>
        /// <returns>The provider send result.</returns>
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

        /// <summary>Gets the configured provider's remaining SMS count.</summary>
        /// <returns>The provider-formatted usage string.</returns>
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
        /// Estimates the number of provider message segments required for a recipient count.
        /// </summary>
        /// <param name="phoneCount">The number of recipients.</param>
        /// <param name="content">The template used to estimate rendered length.</param>
        /// <returns>The estimated number of SMS segments.</returns>
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
