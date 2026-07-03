/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SmsSetting.cs
    文件功能描述：SmsSetting 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
