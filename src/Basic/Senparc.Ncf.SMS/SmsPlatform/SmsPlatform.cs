/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SmsPlatform.cs
    File description: SmsPlatform related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview13 Updated SMS integration support and related localization metadata

----------------------------------------------------------------*/

using System;
using System.Text;

namespace Senparc.Ncf.SMS
{
    public interface ISmsPlatform
    {
        string SmsServiceAddress { get; }
        SenparcSmsSetting Setting { get; set; }

        /// <summary>
        /// send
        /// </summary>
        /// <param name="content"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        SmsResult Send(string content, string number);
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        string GetLastCount();
    }

    public abstract class SmsPlatform : ISmsPlatform
    {
        public virtual string SmsServiceAddress { get; set; }
        public SenparcSmsSetting Setting { get; set; }
        public abstract SmsResult Send(string content, string number);
        public abstract string GetLastCount();

        public SmsPlatform(SmsPlatformType smsPlatformType, string smsServiceAddress, string smsAccountCORPID, string smsAccountName, string smsAccountPassword, string smsAccountSubNumber)
        {
            Setting = new SenparcSmsSetting()
            {
                SmsPlatformType = smsPlatformType,
                SmsAccountCORPID = smsAccountCORPID,
                SmsAccountName = smsAccountName,
                SmsAccountPassword = smsAccountPassword,
                SmsAccountSubNumber = smsAccountSubNumber,
            };
            SmsServiceAddress = smsServiceAddress;
        }


        protected string UrlEncode(string url, Encoding encoding)
        {
            StringBuilder sb = new StringBuilder();
            byte[] bytes = encoding.GetBytes(url);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(@"%" + Convert.ToString(bytes[i], 16));
            }
            return (sb.ToString());
        }
    }
}
