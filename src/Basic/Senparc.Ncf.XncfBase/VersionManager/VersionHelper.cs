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
     1. Please write me a regular expression that can identify the software version number.  
     2. Create an independent class: VersionInfo, used to store version information  
     3. Write a complete unit test for this regular expression that covers various versions as much as possible (use the unit test that comes with .NET).
     */

    public static class VersionHelper
    {
        //This regular expression matches version numbers that follow the semantic versioning specification (semver). It includes a major version number, a minor version number, a revision number, an optional build number, an optional pre-release tag, and an optional metadata tag.
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+))?(?:\+([a-zA-Z\d\-]+))?$";
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z\d\-]+))?(?:\.(\d+))?(?:\+([a-zA-Z\d\-]+))?$";
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-]+))?$";
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?$";
        //private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?$";
        private const string VersionRegex = @"^(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?$";


        //private const string VersionInCodeRegex = @"Version\s*=>\s*""(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?""";
        private const string VersionInCodeRegex = @"Version\s*=>\s*""(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-([a-zA-Z\d\-]+(\.\d+)?))?(?:\+([a-zA-Z\d\-.]+))?""";

        /// <summary>  
        /// Parses the version string and returns a VersionInfo object.  
        /// </summary>  
        /// <param name="versionString">The version string to parse. </param>  
        /// <returns> A VersionInfo object representing the parsed version information. </returns>  
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
                Patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0, // If no patch exists, the default value 0 is used  
                Build = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : (int?)null,
                PreRelease = match.Groups[5].Success ? match.Groups[5].Value : null,
                Metadata = match.Groups[7].Success ? match.Groups[7].Value : null
            };

            return versionInfo;
        }


        /// <summary>
        /// Get the version number from the Register.cs code
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
            var patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0; // If no patch exists, the default value 0 is used  

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
        /// Replace version number
        /// </summary>
        /// <param name="code"></param>
        /// <param name="rawVersionString">Original version positioning string, such as: <code>Version => "0.1.1"</code></param>
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
            // Parse C# code with Roslyn  
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

            // Find the node where the Version property is located  
            PropertyDeclarationSyntax versionProperty = root.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.Text == "Version");

            if (versionProperty != null)
            {
                //Get old version number
                var oldVersionString = versionProperty.ExpressionBody.Expression.ToString().Replace("\"", "");
                Console.WriteLine("oldVersionString:" + oldVersionString);
                var oldVersion = Parse(oldVersionString);

                // If the Version property is found, modify its value  
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

                // Normalize and convert the modified syntax tree to a string and write it back to the original .cs file  
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
