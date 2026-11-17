/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: AutoSendEmailUtility.cs

    File description: AutoSendEmailUtility related implementation

    

    

    Created by: Senparc - 20211124

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Email;
using Senparc.Ncf.Core.Enums;

namespace Senparc.Xncf.EmailExtension
{
    using Senparc.Ncf.Core.Cache;
    using Senparc.Ncf.Core.Extensions;
    using Senparc.Ncf.Log;
    using Senparc.Xncf.EmailExtension.Domain.Cache;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AutoSendEmailThreadUtility
    {
        public static bool OpenAutoSendEmail = true;//settingsautomaticsendstatus
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static bool AutoSendEmailStarted { get; private set; }
        public static int AutoSendCount = 0;//Legacy comment translated to English.
        public static int SendSuccessCount = 0;//Legacy comment translated to English.
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static DateTime NextSendTime { get; set; }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="seconds"></param>
        public void SetSleep(int seconds)
        {
            NextSendTime = DateTime.Now.AddSeconds(seconds);
            Thread.Sleep(seconds * 1000);
        }

        public void SendEventHandler(object state, bool timeout)
        {
            try
            {
                //Legacy comment translated to English.
                if (!OpenAutoSendEmail)
                {
                    AutoSendEmailStarted = false;
                    SetSleep(10);
                    return; //continue;
                }
                AutoSendEmailStarted = true;


                //HttpContext cont = new HttpContext(new HttpRequest("Default.aspx", "http://www.senparc.com/Default.aspx", ""), new HttpResponse(null));

                SendEmailCache emailCache = new SendEmailCache();
                SendEmail sendEmail = new SendEmail(null);
                var emails = emailCache.Data.ToList();//Legacy comment translated to English.
                foreach (var email in emails)
                {
                    if (!OpenAutoSendEmail)
                    {
                        break;
                    }

                    AutoSendCount++;
                    if (email.SendCount > Register.MaxSendEmailTimes)
                    {
                        continue;
                    }
                    bool useBackEmail = email.SendCount >= 3;//Legacy comment translated to English.
                    var accountType = useBackEmail ? EmailAccountType._163 : EmailAccountType.Default;

                    //if (accountType == EmailUserType.Default && email.SendCount == 5 && email.Address.Contains("@163"))
                    //{
                    //    accountType = EmailUserType._163;
                    //}

                    if (email.SendCount > 2)
                    {
                        accountType = EmailAccountType._163;
                    }
                    else if (email.SendCount > 1)
                    {
                        accountType = EmailAccountType.Souidea;
                    }

                    if (sendEmail.DoSendEmail(email.Address, email.Subject, email.Body, email.UserName, false, accountType, true))
                    {
                        emailCache.SendSuccess(email);
                        SendSuccessCount++;
                        LogUtility.EmailLogger.Info($"缓存自动发送Email成功（by {accountType}）：{email.Address}，Subject:{email.Subject}");
                    }
                    else
                    {
                        emailCache.SendFail(email);
                        LogUtility.EmailLogger.Error($"缓存自动发送Email失败（by {accountType}）：{email.Address}，Subject:{email.Subject}");
                    }
                    SetSleep(7);
                }
                if (emailCache.Data.Count == 0)
                {
                    SetSleep(5);
                }
            }
            catch (Exception ex)
            {
                LogUtility.EmailLogger.ErrorFormat("AutoSendEmail发送错误！", ex);
            }
        }

        //public void Send()
        //{
        //    SetSleep(20);
        //    do
        //    {
        //        SendEventHandler(null, false);
        //    } while (true);
        //}

    }
}
