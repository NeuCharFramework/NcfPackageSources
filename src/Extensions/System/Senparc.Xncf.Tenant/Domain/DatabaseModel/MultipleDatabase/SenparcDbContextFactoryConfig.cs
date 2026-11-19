using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.Tanent.Domain.DatabaseModel
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
                    var projectPath = Path.GetFullPath("..\\..\\..\\", AppContext.BaseDirectory);//Legacy comment translated to English.

                    var webPath = Path.GetFullPath("..\\Senparc.Web",/*find the Web directory to obtain the unified database connection-string configuration*/
                                                   projectPath);
                    if (Directory.Exists(webPath))
                    {
                        _rootDictionaryPath = webPath;//prefer the unified Web configuration
                    }
                    _rootDictionaryPath = projectPath;
                }
                return _rootDictionaryPath;
            }
        }
    }
}
