using System;
using System.IO;

namespace Senparc.Xncf.Workspace.Domain.Models.MultipleDatabase;

public static class SenparcDbContextFactoryConfig
{
    private static string? _rootDirectoryPath;

    public static string RootDirectoryPath
    {
        get
        {
            if (_rootDirectoryPath == null)
            {
                var projectPath = Path.GetFullPath($"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}", AppContext.BaseDirectory);
                var webPath = Path.GetFullPath($"..{Path.DirectorySeparatorChar}Senparc.Web", projectPath);
                _rootDirectoryPath = Directory.Exists(webPath) ? webPath : projectPath;
            }

            return _rootDirectoryPath;
        }
    }
}
