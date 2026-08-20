/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AutoSendLogEmail.cs
    文件功能描述：AutoSendLogEmail 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Xml.Linq;

namespace Senparc.Ncf.Core.Email
{
    using Senparc.Ncf.Core.Enums;

    public static class AutoSendLogEmail
    {
        public static void SendLogEmail(Exception e)
        {
            XDocument doc = new XDocument();
            doc.Root.Add(new XElement("ErrorLogo",
                            new XElement("Time", DateTime.Now),
                            new XElement("Exception Message", e.Message),
                            new XElement("InnerException", e.InnerException),
                            new XElement("StackTrace", e.StackTrace)
                            ));
            SendEmail sendExceptionEmail = new SendEmail(SendEmailType.CustomEmail);
            SendEmailParameter_CustomEmail sendEmailParam = new SendEmailParameter_CustomEmail("zsu@senparc.com", "[后台运行]", "Senparc运行错误", doc.ToString());
            sendExceptionEmail.Send(sendEmailParam, true, true, EmailAccountType.Default);
        }
    }
}