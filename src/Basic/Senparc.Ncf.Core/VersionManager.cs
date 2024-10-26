using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Utility.Helpers;
using System;
using System.ComponentModel.Design;
using System.Globalization;
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
            sb.AppendLine($"           NeuCharFramework {ncfVersion}");
            sb.AppendLine($"               Apache License 2.0 ");
            sb.AppendLine("");


            GlobalCulture.
                CultureHelper.SetEnglish(() => {
                    sb.AppendLine("    AI Native / Build-in AI / Domain-Driven Design System");
                    sb.AppendLine("    OpenSource Template：https://github.com/NeuCharFramework/NCF");
                    //sb.AppendLine("    OpenSource Template：https://gitee.com/NeuCharFramework/NCF");
                    sb.AppendLine("    Base Module Source Code：https://github.com/NeuCharFramework/NcfPackageSources");
                    sb.AppendLine("    Document：https://doc.ncf.pub/");
                }),
                GlobalCulture.SetChinese(() => {
                    sb.AppendLine("    AI 原生 / 内置 AI / DDD（Domain-Driven Design） 系统");
                    sb.AppendLine("    开源模板：https://github.com/NeuCharFramework/NCF");
                    sb.AppendLine("    开源模板：https://gitee.com/NeuCharFramework/NCF");
                    sb.AppendLine("    基础模块源码：https://github.com/NeuCharFramework/NcfPackageSources");
                    sb.AppendLine("    文档：https://doc.ncf.pub/");
                })
                );

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
