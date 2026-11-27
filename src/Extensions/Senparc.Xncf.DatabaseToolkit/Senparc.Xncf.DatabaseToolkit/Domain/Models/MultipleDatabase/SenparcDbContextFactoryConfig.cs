using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.DatabaseToolkit.Models.MultipleDatabase
{
    /// <summary>
    /// Resolves the application root used by design-time DatabaseToolkit DbContext factories
    /// and checks candidate roots for the NCF database configuration file.
    /// </summary>
    internal static class SenparcDbContextFactoryConfig
    {
        private static readonly Lazy<string> RootDirectory = new(ResolveRootDirectory);

        public static string RootDictionaryPath => RootDirectory.Value;

        /// <summary>
        /// Searches known development and deployment locations for the database configuration root.
        /// </summary>
        /// <returns>The first matching root, or the application base directory when none matches.</returns>
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

        /// <summary>
        /// Determines whether a candidate root contains the NCF database configuration file.
        /// </summary>
        /// <param name="rootDirectoryPath">The candidate application root.</param>
        /// <returns><see langword="true"/> when the expected configuration file exists.</returns>
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
