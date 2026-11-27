/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: VersionManager.cs
    File description: VersionManager related implementation

    Created by: Senparc - 20200920

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
        /// Builds the localized NCF startup banner, version line, license line,
        /// optional open-source links, and an optional caller-supplied note.
        /// </summary>
        /// <param name="ncfVersion">
        /// The version displayed in the banner. When omitted, the executing
        /// assembly version is used.
        /// </param>
        /// <param name="note">An optional note appended after the standard banner.</param>
        /// <param name="showOpenSourceInfo">
        /// Whether repository and documentation links should be included.
        /// </param>
        /// <returns>The complete localized banner text.</returns>
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
                    sb.AppendLine("    AI 原生 / DDD（Domain-Driven Design）系统");
                    sb.AppendLine("");
                    if (showOpenSourceInfo)
                    {
                        sb.AppendLine("    开源模板：https://github.com/NeuCharFramework/NCF");
                        sb.AppendLine("    开源模板：https://gitee.com/NeuCharFramework/NCF");
                        sb.AppendLine("    文档：https://doc.ncf.pub/");
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

        /// <summary>
        /// Writes the formatted NCF startup banner and success note to the console.
        /// The output culture is selected by the current localization context.
        /// </summary>
        /// <param name="note">The startup note appended below the version information.</param>
        /// <param name="systemVersion">The displayed version; the executing assembly version is used when omitted.</param>
        /// <param name="showOpenSourceInfo">Whether repository and documentation links are included.</param>
        public static void ShowSuccessTip(string note, string systemVersion = null, bool showOpenSourceInfo = true)
        {
            //output the startup success marker
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
