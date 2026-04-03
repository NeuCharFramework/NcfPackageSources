using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    ///AppService log processing
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

            //Make sure it can handle Chinese characters
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
