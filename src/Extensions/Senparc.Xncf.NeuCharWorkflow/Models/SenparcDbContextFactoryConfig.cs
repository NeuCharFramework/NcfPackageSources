/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenparcDbContextFactoryConfig.cs
    文件功能描述：数据模型、DTO 与映射定义


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using System;
using System.IO;

namespace Senparc.Xncf.NeuCharWorkflow.Models;

/// <summary>设计时 Migration 工厂定位宿主配置的路径。</summary>
public static class SenparcDbContextFactoryConfig
{
    private static string? _rootDirectoryPath;

    public static string RootDirectoryPath => _rootDirectoryPath ??= ResolveRootDirectoryPath();

    private static string ResolveRootDirectoryPath()
    {
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
