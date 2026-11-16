/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: WorkspaceOptions.cs
    File description: Workspace storage configuration
    
    
    Created by: Senparc - 20261015
    
    Modified by: Senparc - 20261021
    Change description: v0.1.1 configure the Workspace root path and execution limits

    Modified by: Senparc - 20261102
    Change description: add a default 300 MiB manual upload limit

    Modified by: Senparc - 20261103
    Change description: v0.2.2 add streamed large-file Workspace uploads with tenant-aware ownership checks

----------------------------------------------------------------*/

namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceOptions
{
    public const string SectionName = "SenparcXncfWorkspace";

    public string RootPath { get; set; } = Path.Combine("App_Data", "AIWorkspaces");

    public long MaxWorkspaceBytes { get; set; } = 2L * 1024 * 1024 * 1024;

    public int MaxEntriesPerList { get; set; } = 5000;

    public int MaxFileReadBytes { get; set; } = 2 * 1024 * 1024;

    public int MaxFileWriteBytes { get; set; } = 10 * 1024 * 1024;

    public long MaxUploadFileBytes { get; set; } = 300L * 1024 * 1024;

    public int CommandTimeoutSeconds { get; set; } = 60;

    public int MaxCommandOutputBytes { get; set; } = 2 * 1024 * 1024;

    public int MaxSearchFileBytes { get; set; } = 4 * 1024 * 1024;

    public int MaxSearchFiles { get; set; } = 10_000;

    public int MaxSearchResults { get; set; } = 200;

    public string RuntimeProvider { get; set; } = "NativeAgent";

    public string AgentPath { get; set; } = string.Empty;

    public bool AllowNativeExecution { get; set; }

    public string DockerImage { get; set; } = "senparc/ncf-workspace-agent:0.2.0";

    public int DockerMemoryMegabytes { get; set; } = 512;

    public double DockerCpuLimit { get; set; } = 1.0;

    public string[] AllowedCommands { get; set; } =
        new[] { "dotnet", "git", "node", "npm", "python", "python3", "py", "pytest" };
}
