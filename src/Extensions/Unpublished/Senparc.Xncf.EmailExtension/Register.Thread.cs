/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Thread.cs
    File description: Register.Thread related implementation

    Created by: Senparc - 20211124

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Senparc.Xncf.EmailExtension
{
    public partial class Register : IXncfThread
    {
        private static AutoSendEmailThreadUtility EmailUtility = new AutoSendEmailThreadUtility();
        public void ThreadConfig(XncfThreadBuilder xncfThreadBuilder)
        {
            xncfThreadBuilder.AddThreadInfo(new ThreadInfo(
                name: "Email 自动发送",
                intervalTime: TimeSpan.FromSeconds(3),
                task: async (app, threadInfo) =>
                {
                    EmailUtility.SetSleep(20);
                    EmailUtility.SendEventHandler(null, false);
                }
                ));
        }
    }
}
