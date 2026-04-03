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
        /// Return version information
        /// </summary>
        /// <returns></returns>
        public static string GetVersionNote(string ncfVersion = null, string note = null, bool showOpenSourceInfo = true)
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

            GlobalCulture.Create()
                .SetEnglish(() =>
                {
                    sb.AppendLine("    AI Native / Domain-Driven Design System");
                    sb.AppendLine("");
                    if (showOpenSourceInfo)
                    {

                        sb.AppendLine("    OpenSource Template：https://github.com/NeuCharFramework/NCF");
                        //sb.AppendLine("    OpenSource Template：https://gitee.com/NeuCharFramework/NCF");
                        //sb.AppendLine("    Base Module Source Code：https://github.com/NeuCharFramework/NcfPackageSources");
                        sb.AppendLine("    Document：https://doc.ncf.pub/");
                    }
                })
                .SetChinese(() =>
                {
                    sb.AppendLine("    AI Native / DDD (Domain-Driven Design) System");
                    sb.AppendLine("");
                    if (showOpenSourceInfo)
                    {
                        sb.AppendLine("    Open source template: https://github.com/NeuCharFramework/NCF");
                        sb.AppendLine("    Open source template: https://gitee.com/NeuCharFramework/NCF");
                        //sb.AppendLine("Basic module source code: https://github.com/NeuCharFramework/NcfPackageSources");
                        sb.AppendLine("    Documentation: https://doc.ncf.pub/");
                    }
                })
                .InvokeDefault();

            sb.AppendLine("");
            if (!note.IsNullOrEmpty())
            {
                sb.AppendLine(note);
                sb.AppendLine("");
            }
            return sb.ToString();
        }

        public static void ShowSuccessTip(string note, string systemVersion = null, bool showOpenSourceInfo = true)
        {
            // Output startup success flag
            systemVersion ??= Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var startupNote = Senparc.Ncf.Core.VersionManager.GetVersionNote(systemVersion, note, showOpenSourceInfo);
            Console.WriteLine("----------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(startupNote);
            Console.ResetColor();
            Console.WriteLine("----------------------------------------------------------");

        }
    }
}
