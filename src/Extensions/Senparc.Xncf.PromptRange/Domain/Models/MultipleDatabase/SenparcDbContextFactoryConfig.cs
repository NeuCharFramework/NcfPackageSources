using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.PromptRange.Models
{
    /// <summary>
    /// SenparcDbContextFactory 的公共配置
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string _rootDirectoryPath = null;

        /// <summary>
        /// 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息
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
                            Path.Combine(directory.FullName, "tools", "NcfSimulatedSite", "Senparc.Web")
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