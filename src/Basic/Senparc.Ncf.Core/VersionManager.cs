using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{
    public class VersionManager
    {
        /// <summary>
        /// 返回版本信息
        /// </summary>
        /// <returns></returns>
        public static string GetVersionNote(string ncfVersion,string note=null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("                                         ");
            sb.AppendLine("           _______  ______  _______  　　");
            sb.AppendLine("          |    |  ||      ||    ___| 　　");
            sb.AppendLine("          |       ||   ---||    ___| 　　");
            sb.AppendLine("          |__|____||______||___|     　　");
            sb.AppendLine("                                         ");
            sb.AppendLine($"          NeuCharFramework {ncfVersion}");
            sb.AppendLine($"             Apache License 2.0 ");
            sb.AppendLine("");
            sb.AppendLine("    https://github.com/NeuCharFramework/Ncf");
            sb.AppendLine("    https://gitee.com/NeuCharFramework/Ncf");
            sb.AppendLine("");
            if (!note.IsNullOrEmpty())
            {
                sb.AppendLine(note);
                sb.AppendLine("");
            }
            return sb.ToString();
        }
    }
}
