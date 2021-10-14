using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// AppService 日志处理
    /// </summary>
    public class AppServiceLogger
    {
        StringBuilder stringBuilder = new StringBuilder();

        public void RecordLog(string msg)
        {
            stringBuilder.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
        }

        public string GetLogs()
        {
            return stringBuilder.ToString();
        }

        public void SaveLogs(string name)
        {
            SenparcTrace.SendCustomLog(name, GetLogs());
        }
    }
}
