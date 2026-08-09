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
