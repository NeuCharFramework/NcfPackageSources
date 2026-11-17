/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SmsPlatformFactory.cs
    File description: SmsPlatformFactory related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/


using Senparc.Ncf.SMS;

namespace Senparc.Ncf.SMS
{
    public static class SmsPlatformFactory
    {
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
