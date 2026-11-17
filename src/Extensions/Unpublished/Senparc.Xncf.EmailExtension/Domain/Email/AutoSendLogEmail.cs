/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoSendLogEmail.cs
    File description: AutoSendLogEmail related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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