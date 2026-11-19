using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.PromptRange.Models
{
    /// <summary>
    /// shared configuration for SenparcDbContextFactory
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string _rootDirectoryPath = null;

        /// <summary>
        /// used to find the App_Data folder and then locate the database connection-string configuration
        /// </summary>
        public static string RootDirectoryPath
        {
            get
            {
                if (_rootDirectoryPath == null)
                {
                    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
                    {
                        foreach (var webPath in new[]
                        {
                            Path.Combine(directory.FullName, "Senparc.Web"),
                            Path.Combine(directory.FullName, "tools", "NcfWebAdminBaseline", "Senparc.Web")
                        })
                        {
                            if (!File.Exists(Path.Combine(webPath, "appsettings.json"))) continue;
                            _rootDirectoryPath = webPath;
                            return _rootDirectoryPath;
                        }
                    }
                    throw new InvalidOperationException("Design-time Senparc.Web configuration was not found.");
                }

                return _rootDirectoryPath;
            }
        }
    }
}