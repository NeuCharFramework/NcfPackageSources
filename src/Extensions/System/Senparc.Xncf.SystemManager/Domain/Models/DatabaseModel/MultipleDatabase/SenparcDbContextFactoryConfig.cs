using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    ///Public configuration for SenparcDbContextFactory
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string _rootDictionaryPath = null;

        /// <summary>
        /// is used to find the App_Data folder to find the database connection string configuration information
        /// </summary>
        public static string RootDictionaryPath
        {
            get
            {
                if (_rootDictionaryPath == null)
                {
                    var projectPath = Path.GetFullPath("..\\..\\..\\", AppContext.BaseDirectory);//Project root directory

                    var webPath = Path.GetFullPath("..\\Senparc.Web",/*Locate the web directory for unified database connection string configuration*/
                                                   projectPath);
                    if (Directory.Exists(webPath))
                    {
                        _rootDictionaryPath = webPath;//Prefer using Web unified configuration
                    }
                    _rootDictionaryPath = projectPath;
                }
                return _rootDictionaryPath;
            }
        }
    }
}
