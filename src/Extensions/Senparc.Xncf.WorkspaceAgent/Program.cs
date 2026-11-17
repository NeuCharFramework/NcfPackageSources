/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Program.cs
    File description: Program related definitions and implementation

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.1 implement connection manager and workspace file system provider

----------------------------------------------------------------*/

using Senparc.Xncf.WorkspaceAgent;

var exitCode = await WorkspaceAgentHost.RunAsync(
    args,
    Console.In,
    Console.Out,
    Console.Error,
    CancellationToken.None);
return exitCode;
