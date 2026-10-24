using Senparc.Xncf.Workspace.Agent;

var exitCode = await WorkspaceAgentHost.RunAsync(
    args,
    Console.In,
    Console.Out,
    Console.Error,
    CancellationToken.None);
return exitCode;
