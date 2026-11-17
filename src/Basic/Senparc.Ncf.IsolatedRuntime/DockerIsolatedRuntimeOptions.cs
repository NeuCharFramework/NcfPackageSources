/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DockerIsolatedRuntimeOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.1.0-preview1 implement connection manager and workspace file system provider

----------------------------------------------------------------*/

namespace Senparc.Ncf.IsolatedRuntime;

public sealed class DockerIsolatedRuntimeOptions
{
    public const string SectionName = "SenparcNcf:IsolatedRuntime:Docker";

    public string Executable { get; set; } = "docker";

    public int ProbeTimeoutSeconds { get; set; } = 8;
}
