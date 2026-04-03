using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.KnowledgeBase.Models
{
    /// <summary>
    ///Public configuration for SenparcDbContextFactory
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string _rootDirectoryPath = null;

        /// <summary>
        /// is used to find the App_Data folder to find the database connection string configuration information
        /// </summary>
        public static string RootDirectoryPath
        {
            get
            {
                if (_rootDirectoryPath == null)
                {
                    var projectPath = Path.GetFullPath($"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}", AppContext.BaseDirectory);//Project root directory

                    var webPath = Path.GetFullPath($"..{Path.DirectorySeparatorChar}Senparc.Web",/*Locate the web directory for unified database connection string configuration*/
                                                   projectPath);
                    if (Directory.Exists(webPath))
                    {
                        _rootDirectoryPath = webPath;//Prefer using Web unified configuration
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
