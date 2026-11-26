/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SmsPlatform_Fissoft.cs
    File description: SmsPlatform_Fissoft related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview13 Updated SMS integration support and related localization metadata

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.SMS;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace Senparc.Ncf.SMS
{
    public class SmsPlatform_Fissoft : SmsPlatform, ISmsPlatform
    {
        /// <summary>
        /// Initializes the Fissoft SMS provider with account credentials.
        /// </summary>
        /// <param name="smsServiceAddress">An optional endpoint override.</param>
        /// <param name="smsAccountCORPID">The provider account identifier.</param>
        /// <param name="smsAccountName">The provider account name.</param>
        /// <param name="smsAccountPassword">The provider account password.</param>
        /// <param name="smsAccountSubNumber">The provider sub-number.</param>
        public SmsPlatform_Fissoft(string smsServiceAddress, string smsAccountCORPID, string smsAccountName, string smsAccountPassword, string smsAccountSubNumber)
            : base(SmsPlatformType.Fissoft, smsServiceAddress, smsAccountCORPID, smsAccountName, smsAccountPassword, smsAccountSubNumber)
        {

        }

        /// <summary>
        /// Gets the fixed Fissoft endpoint used by the legacy provider API.
        /// </summary>
        public override string SmsServiceAddress => "http://api.fissoft.com/pubservice";

        /// <inheritdoc cref="ISmsPlatform.Send(string, string)"/>
        /// <remarks>
        /// Long content is split into provider-sized segments. Provider or transport
        /// failures are logged and returned as <see cref="SmsResult.未知错误"/>.
        /// </remarks>
        public override SmsResult Send(string content, string number)
        {
            SmsResult smsResult = SmsResult.未知错误;
            try
            {

                Encoding encoding = Encoding.UTF8;
                var wc = new WebClient();
                wc.Encoding = encoding;
                int contentIndex = 0;
                int limitedLetterCount = 65 * 4;

                List<string> messageList = new List<string>();
                while (contentIndex < content.Length)
                {
                    string limitedContent = content.Substring(contentIndex, Math.Min(limitedLetterCount, content.Length - contentIndex));
                    messageList.Add(limitedContent);
                    contentIndex += limitedLetterCount;
                }

                for (int i = 0; i < messageList.Count; i++)
                {
                    var msg = messageList[i];
                    if (messageList.Count > 1)
                    {
                        msg = $" [{i + 1}/{messageList.Count}]" + msg;
                    }
                    string encodedLimitedConent = UrlEncode(msg, encoding);



                    var url = string.Format("{0}/SMSSend?OperId={1}&OperPass={2}&Mobiles={3}&Message={4}",
                                    SmsServiceAddress, Setting.SmsAccountCORPID, Setting.SmsAccountPassword, number, encodedLimitedConent);
                    var smsState = wc.DownloadString(url);

                    var fissosoft_SendResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Fissosoft_SendResult>(smsState);
                    if (fissosoft_SendResult.Code == 1)
                    {
                        smsResult = SmsResult.成功;
                    }
                    else
                    {
                        if (!Enum.TryParse(fissosoft_SendResult.Code.ToString(), out smsResult))
                        {
                            smsResult = SmsResult.未知错误;
                        }
                    }
                    Log.LogUtility.SmsLogger.Info($"发送短信成功：{content}，号码：{number}。状态：{smsResult.ToString()}。发送通道：Fissoft");
                    Thread.Sleep(200);
                }
            }
            catch
            {
                Log.LogUtility.SmsLogger.Error($"发送短信失败：{content}，号码：{number}。发送通道：Fissoft");
            }
            finally
            {
            }
            return smsResult;
        }

        /// <inheritdoc />
        public override string GetLastCount()
        {
            var url = string.Format("{0}/SMSAccountInfo?OperId={1}&OperPass={2}",
               SmsServiceAddress, Setting.SmsAccountName, Setting.SmsAccountPassword);

            Encoding encoding = Encoding.UTF8;
            var wc = new WebClient();
            wc.Encoding = encoding;
            var result = wc.DownloadString(url);
            var lastCountResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Fissoft_LastCountResult>(result);
            var value = Newtonsoft.Json.JsonConvert.DeserializeObject<Fissoft_LastCountResult_Value>(lastCountResult.Value);
            return value.SMSNum.ToString();
        }
    }

    /// <summary>
    /// Deserializes the status returned by the Fissoft send endpoint.
    /// </summary>
    public class Fissosoft_SendResult
    {
        /// <summary>Gets or sets the provider result code.</summary>
        public int Code { get; set; }
        /// <summary>Gets or sets the provider result message.</summary>
        public string Msg { get; set; }
    }


    /// <summary>
    /// Deserializes the account-status envelope returned by the Fissoft provider.
    /// </summary>
    public class Fissoft_LastCountResult
    {
        /// <summary>Gets or sets the provider result code.</summary>
        public int Code { get; set; }
        /// <summary>Gets or sets the provider result message.</summary>
        public string Msg { get; set; }
        /// <summary>Gets or sets the serialized account-status payload.</summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// Deserializes the account usage payload returned by the Fissoft provider.
    /// </summary>
    public class Fissoft_LastCountResult_Value
    {
        /// <summary>Gets or sets the provider account name.</summary>
        public string UserName { get; set; }
        /// <summary>Gets or sets the provider operator identifier.</summary>
        public string OperId { get; set; }
        /// <summary>Gets or sets the remaining SMS count.</summary>
        public int SMSNum { get; set; }
        /// <summary>Gets or sets the remaining emergency SMS count.</summary>
        public int EmergentSMSNum { get; set; }
        /// <summary>Gets or sets whether the provider account is enabled.</summary>
        public int IsEnable { get; set; }
    }

}
