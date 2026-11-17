/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SmsSetting.cs

    File description: SmsSetting related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

namespace Senparc.Ncf.SMS
{
    public class SenparcSmsSetting
    {
        public string SmsAccountCORPID { get; set; }
        public string SmsAccountName { get; set; }
        public string SmsAccountSubNumber { get; set; }
        public string SmsAccountPassword { get; set; }
        public SmsPlatformType SmsPlatformType { get; set; }

    }
}
