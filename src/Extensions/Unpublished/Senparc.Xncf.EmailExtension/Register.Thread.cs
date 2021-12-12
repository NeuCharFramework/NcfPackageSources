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
