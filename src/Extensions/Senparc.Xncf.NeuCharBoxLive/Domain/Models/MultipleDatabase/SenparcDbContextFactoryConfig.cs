/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SenparcDbContextFactoryConfig.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：SenparcDbContextFactoryConfig


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System;
using System.IO;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    /// <summary>
    /// SenparcDbContextFactory 的公共配置
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string? _rootDirectoryPath = null;

        /// <summary>
        /// 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息
        /// </summary>
        public static string RootDirectoryPath => _rootDirectoryPath ??= ResolveRootDirectoryPath();

        private static string ResolveRootDirectoryPath()
        {
            // NcfPackageSources layout: walk up to tools/NcfSimulatedSite/Senparc.Web (or a sibling Senparc.Web).
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
                {
                    var simulatedSite = Path.Combine(directory.FullName, "tools", "NcfSimulatedSite", "Senparc.Web");
                    if (Directory.Exists(simulatedSite))
                    {
                        return simulatedSite;
                    }
                    var siblingWeb = Path.Combine(directory.FullName, "Senparc.Web");
                    if (Directory.Exists(siblingWeb))
                    {
                        return siblingWeb;
                    }
                }
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
