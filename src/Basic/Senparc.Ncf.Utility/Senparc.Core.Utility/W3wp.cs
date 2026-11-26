/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: W3wp.cs
    File description: W3wp related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

//using System.Management;

namespace Senparc.Ncf.Core.Utility
{
    public class W3wp
    {
        /// <summary>
        /// Prevents instantiation of the legacy static-helper container.
        /// </summary>
        private W3wp() { }

        //Legacy comment translated to English.

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
