/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkspaceOptions.cs
    文件功能描述：工作区存储配置
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.1 配置工作区根路径与执行限制

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

    public int CommandTimeoutSeconds { get; set; } = 60;

    public int MaxCommandOutputBytes { get; set; } = 2 * 1024 * 1024;

    public int MaxSearchFileBytes { get; set; } = 4 * 1024 * 1024;

    public int MaxSearchFiles { get; set; } = 10_000;

    public int MaxSearchResults { get; set; } = 200;

    public string[] AllowedCommands { get; set; } =
        new[] { "dotnet", "git", "node", "npm", "python", "python3", "py", "pytest" };
}
