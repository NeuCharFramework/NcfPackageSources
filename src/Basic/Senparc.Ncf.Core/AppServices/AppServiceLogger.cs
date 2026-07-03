/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AppServiceLogger.cs
    文件功能描述：AppServiceLogger 相关实现
    
    
    创建标识：Senparc - 20211014
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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

            //确保能够处理中文字符
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
