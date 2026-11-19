using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.WeixinManager.Domain.Models.MultipleDatabase
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    internal static class SenparcDbContextFactoryConfig
    {
        private static readonly Lazy<string> RootDirectory = new(ResolveRootDirectory);

        public static string RootDictionaryPath => RootDirectory.Value;

        private static string ResolveRootDirectory()
        {
            var projectPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var candidates = new List<string>
            {
                Path.GetFullPath(Path.Combine(projectPath, "..", "Senparc.Web")),
                projectPath
            };

            for (var directory = new DirectoryInfo(projectPath); directory != null; directory = directory.Parent)
            {
                candidates.Add(Path.Combine(directory.FullName, "tools", "NcfWebAdminBaseline", "Senparc.Web"));
                candidates.Add(Path.Combine(directory.FullName, "src", "back-end", "Senparc.Web"));
            }

            return candidates.FirstOrDefault(HasDatabaseConfiguration) ?? projectPath;
        }

        private static bool HasDatabaseConfiguration(string rootDirectoryPath)
        {
            return File.Exists(Path.Combine(
                rootDirectoryPath,
                "App_Data",
                "Database",
                "SenparcConfig.config"));
        }
    }
}
