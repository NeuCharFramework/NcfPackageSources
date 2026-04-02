using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.XncfModuleManager.Models
{
    /// <summary>
    /// SenparcDbContextFactory common configuration.
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string _rootDictionaryPath = null;

        /// <summary>
        /// Used to locate the App_Data folder to find database connection string configuration.
        /// </summary>
        public static string RootDictionaryPath
        {
            get
            {
                if (_rootDictionaryPath == null)
                {
                    var projectPath = Path.GetFullPath("..\\..\\..\\", AppContext.BaseDirectory);//project root directory

                    var webPath = Path.GetFullPath("..\\Senparc.Web",/*locate the Web directory to use unified database connection string configuration*/
                                                   projectPath);
                    if (Directory.Exists(webPath))
                    {
                        _rootDictionaryPath = webPath;//prefer unified Web configuration
                    }
                    _rootDictionaryPath = projectPath;
                }
                return _rootDictionaryPath;
            }
        }
    }
}
