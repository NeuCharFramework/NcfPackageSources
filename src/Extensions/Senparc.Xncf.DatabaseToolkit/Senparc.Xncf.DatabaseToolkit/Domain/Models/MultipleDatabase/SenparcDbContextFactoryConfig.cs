using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.DatabaseToolkit.Models.MultipleDatabase
{
    /// <summary>
    /// 设计时工厂共享配置。仅选择实际包含 SenparcConfig.config 的站点目录，
    /// 同时兼容源码仓库、传统相邻项目和发布目录结构。
    /// </summary>
    internal static class SenparcDbContextFactoryConfig
    {
        private static readonly Lazy<string> RootDirectory = new(ResolveRootDirectory);

        public static string RootDictionaryPath => RootDirectory.Value;

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
                candidates.Add(Path.Combine(directory.FullName, "tools", "NcfSimulatedSite", "Senparc.Web"));
                candidates.Add(Path.Combine(directory.FullName, "src", "back-end", "Senparc.Web"));
            }

            return candidates.FirstOrDefault(HasDatabaseConfiguration) ?? projectPath;
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
