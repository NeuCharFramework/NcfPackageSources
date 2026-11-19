/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LogUtility.cs
    File description: LogUtility related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.2-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using log4net;

namespace Senparc.Ncf.Log
{
    public static partial class LogUtility
    {
        public static object LogLock = new object();

        public static ILog WebLogger => GetLogger("WebLogger");
        public static ILog Cache => GetLogger("Cache");
        public static ILog OperationQueue => GetLogger("OperationQueue");
        public static ILog EmailLogger => GetLogger("EmailLogger");
        public static ILog SystemLogger => GetLogger("SystemLogger");
        public static ILog AccountPayLog => GetLogger("AccountPayLog");
        public static ILog SmsLogger => GetLogger("SmsLogger");
        public static ILog Account => GetLogger("Account");
        public static ILog AdminUserInfo => GetLogger("AdminUserInfo");
        public static ILog WeixinOAuth => GetLogger("WeixinOAuth");
        public static ILog TrackPageLoadPerformance => GetLogger("TrackPageLoadPerformance");
        public static ILog Weixin => GetLogger("Weixin");


        public static int Int { get; set; }
        //Legacy comment translated to English.

        public static ILog GetLogger(string name)
        {
            lock (LogLock)
            {
                //var appenders = LogManager.GetRepository().GetAppenders();
                //if (appenders.Count() == 0)
                //{
                //Legacy comment translated to English.
                //    return log4net.LogManager.GetLogger(name);
                //}

                //var fileAppender = appenders.First(appender => appender.Name == "SysLogAppender") as RollingFileAppender;
                //fileAppender.File = "App_Data/";
                //fileAppender.ActivateOptions();
                //var repository = LogManager.GetRepository(name) ?? LogManager.CreateRepository(name);
                return LogManager.GetLogger("NETCoreRepository", name);
            }
        }
    }
}
