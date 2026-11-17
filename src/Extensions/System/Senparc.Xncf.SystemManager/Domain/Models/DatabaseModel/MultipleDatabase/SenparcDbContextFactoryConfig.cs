using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    /// shared configuration for SenparcDbContextFactory
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string _rootDictionaryPath = null;

        /// <summary>
        /// used to find the App_Data folder and then locate the database connection-string configuration
        /// </summary>
        public static string RootDictionaryPath
        {
            get
            {
                if (_rootDictionaryPath == null)
                {
                    var projectPath = Path.GetFullPath(
                        Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    var candidates = new List<string>
                    {
                        Path.GetFullPath(Path.Combine(projectPath, "..", "Senparc.Web")),
                        projectPath
                    };

                    for (var directory = new DirectoryInfo(projectPath); directory != null; directory = directory.Parent)
                    {
                        candidates.Add(Path.Combine(directory.FullName, "tools", "NcfSimulatedSite", "Senparc.Web"));
                        candidates.Add(Path.Combine(directory.FullName, "src", "back-end", "Senparc.Web"));
                    }

                    _rootDictionaryPath = candidates.FirstOrDefault(HasDatabaseConfiguration) ?? projectPath;
                }
                return _rootDictionaryPath;
            }
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
