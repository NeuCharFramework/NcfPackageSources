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
        /// Sends one SMS message to the supplied destination number.
        /// </summary>
        /// <param name="content">The message text to send.</param>
        /// <param name="number">The destination phone number.</param>
        /// <returns>The provider-specific send result.</returns>
        SmsResult Send(string content, string number);
        /// <summary>
        /// Queries the provider for its current remaining or usage count.
        /// </summary>
        /// <returns>A provider-formatted usage string.</returns>
        string GetLastCount();
    }

    public abstract class SmsPlatform : ISmsPlatform
    {
        public virtual string SmsServiceAddress { get; set; }
        public SenparcSmsSetting Setting { get; set; }
        /// <summary>
        /// Sends an SMS message through the concrete provider implementation.
        /// </summary>
        /// <param name="content">The message text to send.</param>
        /// <param name="number">The destination phone number.</param>
        /// <returns>The provider-specific send result.</returns>
        public abstract SmsResult Send(string content, string number);

        /// <summary>
        /// Gets the provider-specific remaining or usage count.
        /// </summary>
        /// <returns>A provider-formatted usage string.</returns>
        public abstract string GetLastCount();

        /// <summary>
        /// Initializes provider settings used by the concrete SMS implementation.
        /// </summary>
        /// <param name="smsPlatformType">The provider type represented by the instance.</param>
        /// <param name="smsServiceAddress">The provider endpoint, when configured.</param>
        /// <param name="smsAccountCORPID">The provider account or corporation identifier.</param>
        /// <param name="smsAccountName">The provider account name.</param>
        /// <param name="smsAccountPassword">The provider account password.</param>
        /// <param name="smsAccountSubNumber">The provider sub-number.</param>
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

        /// <summary>
        /// Percent-encodes text using the bytes produced by the selected encoding.
        /// </summary>
        /// <param name="url">The text to encode.</param>
        /// <param name="encoding">The encoding used to convert text to bytes.</param>
        /// <returns>A percent-encoded representation of the input bytes.</returns>
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
