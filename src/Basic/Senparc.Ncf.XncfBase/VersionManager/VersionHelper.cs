using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Senparc.Ncf.XncfBase.VersionManager
{
    /* GPT-4 Prompt:
     1、请为我编写一个能够识别软件版本号的正则表达式  
     2、创建一个独立的类：VersionInfo，用于储存版本信息  
     3、为这个正则表达式编写一个尽量涵盖各种版本情况的完整的单元测试（使用 .NET 自带的单元测试）。
     */

    public static class VersionHelper
    {
        //这个正则表达式匹配遵循语义化版本规范（semver）的版本号。它包括主版本号、次版本号、修订号、可选的构建号、可选的预发布版本标签和可选的元数据标签。
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+))?(?:\+([a-zA-Z\d\-]+))?$";
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z\d\-]+))?(?:\.(\d+))?(?:\+([a-zA-Z\d\-]+))?$";
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-]+))?$";
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?$";
        private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?$";

        private const string VersionInCodeRegex = @"Version\s*=>\s*""(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?""";


        /// <summary>  
        /// 解析版本字符串并返回一个 VersionInfo 对象。  
        /// </summary>  
        /// <param name="versionString">要解析的版本字符串。</param>  
        /// <returns>表示解析后的版本信息的 VersionInfo 对象。</returns>  
        public static VersionInfo Parse(string versionString)
        {
            var regex = new Regex(VersionRegex);
            var match = regex.Match(versionString);

            if (!match.Success)
            {
                throw new ArgumentException("无效的版本字符串格式。", nameof(versionString));
            }

            var versionInfo = new VersionInfo
            {
                Major = int.Parse(match.Groups[1].Value),
                Minor = int.Parse(match.Groups[2].Value),
                Patch = int.Parse(match.Groups[3].Value),
                Build = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : (int?)null,
                PreRelease = match.Groups[5].Success ? match.Groups[5].Value : null,
                Metadata = match.Groups[7].Success ? match.Groups[7].Value : null
            };

            return versionInfo;
        }

        /// <summary>
        /// 从 Register.cs 代码中获取版本号
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static VersionInfo ParseFromCode(string code)
        {
            var regex = new Regex(VersionInCodeRegex);
            var match = regex.Match(code);

            if (!match.Success)
            {
                throw new ArgumentException("无法从代码中找到有效的版本字符串。", nameof(code));
            }

            var versionString = $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";

            if (match.Groups[4].Success)
            {
                versionString += $".{match.Groups[4].Value}";
            }

            if (match.Groups[5].Success)
            {
                versionString += $"-{match.Groups[5].Value}";
            }

            if (match.Groups[7].Success)
            {
                versionString += $"+{match.Groups[7].Value}";
            }

            return Parse(versionString);
        }

        /// <summary>
        /// 替换版本号
        /// </summary>
        /// <param name="code"></param>
        /// <param name="oldVersionInfo"></param>
        /// <param name="newVersionInfo"></param>
        /// <returns></returns>
        public static string ReplaceVersionInCode(string code, VersionInfo oldVersionInfo, VersionInfo newVersionInfo)
        {
            var oldVersionString = oldVersionInfo.ToString();
            var newVersionString = newVersionInfo.ToString();

            var regex = new Regex(Regex.Escape($"Version => \"{oldVersionString}\""));
            var replacedCode = regex.Replace(code, $"Version => \"{newVersionString}\"");

            return replacedCode;
        }
    }

}
