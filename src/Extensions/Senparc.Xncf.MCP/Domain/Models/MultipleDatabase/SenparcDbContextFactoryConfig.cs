using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.MCP.Models
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
                    var projectPath = Path.GetFullPath($"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}", AppContext.BaseDirectory);//Legacy comment translated to English.

                    var webPath = Path.GetFullPath($"..{Path.DirectorySeparatorChar}Senparc.Web",/*find the Web directory to obtain the unified database connection-string configuration*/
                                                   projectPath);
                    if (Directory.Exists(webPath))
                    {
                        _rootDirectoryPath = webPath;//prefer the unified Web configuration
                    }
                    else
                    {
                        _rootDirectoryPath = projectPath;
                    }
                }
                return _rootDirectoryPath;
            }
        }
    }
}
