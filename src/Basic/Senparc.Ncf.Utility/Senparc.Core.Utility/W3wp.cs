/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：W3wp.cs
    文件功能描述：W3wp 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

//using System.Management;

namespace Senparc.Ncf.Core.Utility
{
    public class W3wp
    {
        private W3wp() { }

        //COCONET .net core不支持System.Management，需要继续改进

        //public static string GetAllW3wp(string input)
        //{
        //    System.Management.ObjectQuery oQuery = new System.Management.ObjectQuery("select * from Win32_Process where Name='w3wp.exe'");
        //    ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oQuery);
        //    ManagementObjectCollection oReturnCollection = oSearcher.Get();

        //    string pid;
        //    string cmdLine;
        //    StringBuilder sb = new StringBuilder();
        //    foreach (ManagementObject oReturn in oReturnCollection)
        //    {
        //        pid = oReturn.GetPropertyValue("ProcessId").ToString();
        //        cmdLine = (string)oReturn.GetPropertyValue("CommandLine");
        //        string pattern = "-ap \"(.*)\"";
        //        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
        //        Match match = regex.Match(cmdLine);
        //        string appPoolName = match.Groups[1].ToString();
        //        sb.AppendFormat("W3WP.exe PID: {0} AppPoolId:{1}\r\n", pid, appPoolName);
        //    }
        //    return sb.ToString();
        //}

        //public static string GetAppPollName(string input)
        //{
        //    System.Management.ObjectQuery oQuery = new System.Management.ObjectQuery("select * from Win32_Process where Name='w3wp.exe'");
        //    ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oQuery);
        //    ManagementObjectCollection oReturnCollection = oSearcher.Get();

        //    string pid;
        //    string cmdLine;
        //    string appPollNameResult = null;
        //    foreach (ManagementObject oReturn in oReturnCollection)
        //    {
        //        pid = oReturn.GetPropertyValue("ProcessId").ToString();
        //        cmdLine = (string)oReturn.GetPropertyValue("CommandLine");
        //        string pattern = "-ap \"(.*)\"";
        //        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
        //        Match match = regex.Match(cmdLine);
        //        string appPoolName = match.Groups[1].ToString();
        //        appPollNameResult = appPoolName.Substring(0, appPoolName.IndexOf(@""""));
        //        break;
        //    }
        //    return appPollNameResult;
        //}
    }
}
