/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: AppServiceLogger.cs

    File description: AppServiceLogger related implementation

    

    

    Created by: Senparc - 20211014

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// AppService Log Handling
    /// </summary>
    public class AppServiceLogger
    {
        StringBuilder stringBuilder = new StringBuilder();

        public string Append()
        {
            stringBuilder.AppendLine("");
            return "";
        }

        public string Append(string msg)
        {
            stringBuilder.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
            return msg;
        }

        public string GetLogs()
        {
            return stringBuilder.ToString();
        }

        public void SaveLogs(string name)
        {
            string logs = GetLogs();

            //Ensure that Chinese characters can be handled
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(logs);
            string utf8String = Encoding.UTF8.GetString(utf8Bytes);

            SenparcTrace.SendCustomLog(name, utf8String);
        }

        public override string ToString()
        {
            return GetLogs();
        }
    }
}
