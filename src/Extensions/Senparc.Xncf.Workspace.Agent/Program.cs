/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Program.cs
    文件功能描述：Program 相关定义与实现


    创建标识：Senparc - 20261024

    修改标识：Senparc - 20261103
    修改描述：v0.2.1 implement connection manager and workspace file system provider

----------------------------------------------------------------*/

using Senparc.Xncf.Workspace.Agent;

var exitCode = await WorkspaceAgentHost.RunAsync(
    args,
    Console.In,
    Console.Out,
    Console.Error,
    CancellationToken.None);
return exitCode;
