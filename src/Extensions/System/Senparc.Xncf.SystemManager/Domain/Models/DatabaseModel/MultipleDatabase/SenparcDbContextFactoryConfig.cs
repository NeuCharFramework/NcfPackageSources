using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    /// SenparcDbContextFactory 的公共配置
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string _rootDictionaryPath = null;

        /// <summary>
        /// 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息
        /// </summary>
        public static string RootDictionaryPath
        {
            get
            {
                if (_rootDictionaryPath == null)
                {
                    var projectPath = Path.GetFullPath(
                        Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

                    // 源码仓库、传统相邻项目和发布目录的结构不同。设计时只选择
                    // 真正包含数据库配置的目录，避免在 macOS/Linux 上因反斜杠路径失效。
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
