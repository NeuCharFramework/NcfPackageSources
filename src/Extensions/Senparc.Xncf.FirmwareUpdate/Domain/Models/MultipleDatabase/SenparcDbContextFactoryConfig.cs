namespace Senparc.Xncf.FirmwareUpdate.Models;

/// <summary>
/// Resolves the application root directory used by the FirmwareUpdate design-time
/// database context factory.
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
                // Prefer the sibling web application when available so design-time
                // configuration follows the same content root as the host.
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
