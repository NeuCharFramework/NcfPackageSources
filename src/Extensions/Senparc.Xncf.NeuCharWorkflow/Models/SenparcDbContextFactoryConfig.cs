/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcDbContextFactoryConfig.cs
    File description: Data models, DTOs, and mapping definitions

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 improve workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.IO;

namespace Senparc.Xncf.NeuCharWorkflow.Models;

/// <summary>Design-time migration factory that locates the host configuration path.</summary>
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
