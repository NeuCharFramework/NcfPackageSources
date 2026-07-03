/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Thread.cs
    文件功能描述：Register.Thread 相关实现
    
    
    创建标识：Senparc - 20211124
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
