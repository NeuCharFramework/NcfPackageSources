namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceOptions
{
    public const string SectionName = "SenparcXncfWorkspace";

    public string RootPath { get; set; } = Path.Combine("App_Data", "AIWorkspaces");

    public long MaxWorkspaceBytes { get; set; } = 2L * 1024 * 1024 * 1024;

    public int MaxEntriesPerList { get; set; } = 5000;

    public int MaxFileReadBytes { get; set; } = 2 * 1024 * 1024;

    public int MaxFileWriteBytes { get; set; } = 10 * 1024 * 1024;

    public int CommandTimeoutSeconds { get; set; } = 60;

    public int MaxCommandOutputBytes { get; set; } = 2 * 1024 * 1024;

    public int MaxSearchFileBytes { get; set; } = 4 * 1024 * 1024;

    public int MaxSearchFiles { get; set; } = 10_000;

    public int MaxSearchResults { get; set; } = 200;

    public string[] AllowedCommands { get; set; } =
        new[] { "dotnet", "git", "node", "npm", "python", "python3", "py", "pytest" };
}
