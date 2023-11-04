using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net.WebSockets;
using Senparc.Ncf.Core.Exceptions;

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
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?$";
        private const string VersionRegex = @"^(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?$";


        //private const string VersionInCodeRegex = @"Version\s*=>\s*""(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?""";
        private const string VersionInCodeRegex = @"Version\s*=>\s*""(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?""";

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
                Patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0, // 如果不存在 Patch，则使用默认值 0  
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
        public static (VersionInfo VersionInfo, string RawVersionString) ParseFromCode(string code)
        {
            var regex = new Regex(VersionInCodeRegex);
            var match = regex.Match(code);

            if (!match.Success)
            {
                throw new ArgumentException("无法从代码中找到有效的版本字符串。", nameof(code));
            }

            var major = int.Parse(match.Groups[1].Value);
            var minor = int.Parse(match.Groups[2].Value);
            var patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0; // 如果不存在 Patch，则使用默认值 0  

            var versionString = $"{major}.{minor}.{patch}";

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

            return (VersionInfo: Parse(versionString), RawVersionString: match.Value);
        }


        /// <summary>
        /// 替换版本号
        /// </summary>
        /// <param name="code"></param>
        /// <param name="rawVersionString">原始版本定位字符串，如：<code>Version => "0.1.1"</code></param>
        /// <param name="newVersionInfo"></param>
        /// <returns></returns>
        public static string ReplaceVersionInCode(string code, string rawVersionString, VersionInfo newVersionInfo)
        {
            var newVersionString = newVersionInfo.ToString();

            var regex = new Regex(Regex.Escape(rawVersionString));
            var replacedCode = regex.Replace(code, $"Version => \"{newVersionString}\"");

            return replacedCode;
        }


        public static string UpdateVersionInCodeWithRoslyn(string fileContent, UpdateVersionType updateType)
        {
            // 使用 Roslyn 解析 C# 代码  
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

            // 查找 Version 属性所在的节点  
            PropertyDeclarationSyntax versionProperty = root.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.Text == "Version");

            if (versionProperty != null)
            {
                //获取旧的版本号
                var oldVersionString = versionProperty.ExpressionBody.Expression.ToString().Replace("\"", "");
                Console.WriteLine("oldVersionString:" + oldVersionString);
                var oldVersion = Parse(oldVersionString);

                // 如果找到了 Version 属性，修改其值  
                var newVersion = new VersionInfo();

                switch (updateType)
                {
                    case UpdateVersionType.NoUpdate:
                        break;
                    case UpdateVersionType.MajorUpdate:
                        newVersion = oldVersion with { Major = oldVersion.Major + 1 };
                        break;
                    case UpdateVersionType.MinorUpdate:
                        newVersion = oldVersion with { Minor = oldVersion.Minor + 1 };
                        break;
                    case UpdateVersionType.PatchUpdate:
                        newVersion = oldVersion with { Patch = oldVersion.Patch + 1 };
                        break;
                    default:
                        throw new NcfExceptionBase("无法识别的版本更新类型");

                }

                ExpressionSyntax newVersionExpression = SyntaxFactory.ParseExpression($"\"{newVersion}\"");
                ArrowExpressionClauseSyntax newExpressionBody = SyntaxFactory.ArrowExpressionClause(newVersionExpression);
                PropertyDeclarationSyntax newVersionProperty = versionProperty.WithExpressionBody(newExpressionBody);

                // 将修改后的语法树规范化并转换为字符串，并写回到原始的 .cs 文件中  
                SyntaxNode newRoot = root.ReplaceNode(versionProperty, newVersionProperty);
                string newContent = newRoot
                    //.NormalizeWhitespace()
                    .ToFullString();

                return newContent;
            }
            return null;
        }
    }
}
