/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SmsPlatformFactory.cs
    文件功能描述：SmsPlatformFactory 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
