namespace Senparc.Xncf.FirmwareUpdate.Models;

/// <summary>
/// SenparcDbContextFactory 的公共配置（设计时迁移）
/// </summary>
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
                if (Directory.Exists(webPath))
                {
                    _rootDirectoryPath = webPath;
                }
                else
                {
                    _rootDirectoryPath = projectPath;
                }
            }
            return _rootDirectoryPath;
        }
    }
}
