/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SmsPlatformFactory.cs
    File description: SmsPlatformFactory related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview13 Updated SMS integration support and related localization metadata

----------------------------------------------------------------*/


using Senparc.Ncf.SMS;

namespace Senparc.Ncf.SMS
{
    public static class SmsPlatformFactory
    {
        /// <summary>
        /// Creates the configured SMS provider implementation for the selected platform.
        /// </summary>
        /// <param name="smsAccountCorpid">The provider account or corporation identifier.</param>
        /// <param name="smsAccountName">The provider account name.</param>
        /// <param name="smsAccountPassword">The provider account password.</param>
        /// <param name="smsAccountSubNumber">The provider sub-number.</param>
        /// <param name="smsPlatformType">The provider type; JunMei is used by default.</param>
        /// <returns>A provider instance initialized with the supplied account settings.</returns>
        public static ISmsPlatform GetSmsPlateform(string smsAccountCorpid, string smsAccountName,
            string smsAccountPassword, string smsAccountSubNumber, SmsPlatformType smsPlatformType = SmsPlatformType.JunMei)
        {
            switch (smsPlatformType)
            {
                case SmsPlatformType.Fissoft:
                    return new SmsPlatform_Fissoft(null, smsAccountCorpid, smsAccountName, smsAccountPassword, smsAccountSubNumber);
                default:
                    return new SmsPlatform_JunMei(null, smsAccountCorpid, smsAccountName, smsAccountPassword, smsAccountSubNumber);
            }
        }
    }
}
