using Senparc.CO2NET.Extensions;
using System;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.Core
{
    public class VersionManager
    {
        /// <summary>
        /// 返回版本信息
        /// </summary>
        /// <returns></returns>
        public static string GetVersionNote(string ncfVersion = null, string note = null)
        {
            ncfVersion ??= Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("             _______  ______  _______");
            sb.AppendLine("            |    |  ||      ||    ___|");
            sb.AppendLine("            |       ||   ---||    ___|");
            sb.AppendLine("            |__|____||______||___|");
            sb.AppendLine("");
            sb.AppendLine($"            NeuCharFramework {ncfVersion}");
            sb.AppendLine($"               Apache License 2.0 ");
            sb.AppendLine("");
            sb.AppendLine("    开源模板：https://github.com/NeuCharFramework/NCF");
            sb.AppendLine("    开源模板：https://gitee.com/NeuCharFramework/NCF");
            sb.AppendLine("    文档：https://www.ncf.pub/docs/");
            sb.AppendLine("");
            if (!note.IsNullOrEmpty())
            {
                sb.AppendLine(note);
                sb.AppendLine("");
            }
            return sb.ToString();
        }

        public static void ShowSuccessTip(string note)
        {
            //输出启动成功标志
            var systemVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var startupNote = Senparc.Ncf.Core.VersionManager.GetVersionNote(systemVersion, note);
            Console.WriteLine("----------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(startupNote);
            Console.ResetColor();
            Console.WriteLine("----------------------------------------------------------");

        }
    }
}
