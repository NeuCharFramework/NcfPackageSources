/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoSendEmailUtility.cs
    File description: AutoSendEmailUtility related implementation

    Created by: Senparc - 20211124

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

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
    /// Coordinates the legacy background loop that drains queued email messages
    /// and retries delivery through configured accounts.
    /// </summary>
    public class AutoSendEmailThreadUtility
    {
        /// <summary>
        /// Gets or sets whether automatic queued-email processing is enabled.
        /// </summary>
        public static bool OpenAutoSendEmail = true;

        /// <summary>
        /// Gets whether the background sender has entered its processing loop.
        /// </summary>
        public static bool AutoSendEmailStarted { get; private set; }

        /// <summary>
        /// Gets the number of queued email records examined by the sender.
        /// </summary>
        public static int AutoSendCount = 0;

        /// <summary>
        /// Gets the number of queued email records sent successfully.
        /// </summary>
        public static int SendSuccessCount = 0;

        /// <summary>
        /// Gets or sets the next time at which the sender is expected to resume processing.
        /// </summary>
        public static DateTime NextSendTime { get; set; }


        /// <summary>
        /// Records the next wake-up time and blocks the current thread for the
        /// requested delay.
        /// </summary>
        /// <param name="seconds">The delay in seconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Propagated by <see cref="Thread.Sleep(int)"/> when the calculated
        /// millisecond delay is invalid.
        /// </exception>
        public void SetSleep(int seconds)
        {
            NextSendTime = DateTime.Now.AddSeconds(seconds);
            Thread.Sleep(seconds * 1000);
        }

        /// <summary>
        /// Processes queued email records when automatic sending is enabled.
        /// Delivery failures are recorded in the cache and logged; they do not
        /// abort processing of the remaining records.
        /// </summary>
        /// <param name="state">The timer callback state, retained for compatibility.</param>
        /// <param name="timeout">Whether the timer callback was triggered by a timeout.</param>
        public void SendEventHandler(object state, bool timeout)
        {
            try
            {
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
                var emails = emailCache.Data.ToList();
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
                    bool useBackEmail = email.SendCount >= 3;
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
