/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DockerIsolatedRuntimeOptions.cs
    文件功能描述：共享 Docker 隔离运行时配置

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

namespace Senparc.Ncf.IsolatedRuntime;

public sealed class DockerIsolatedRuntimeOptions
{
    public const string SectionName = "SenparcNcf:IsolatedRuntime:Docker";

    public string Executable { get; set; } = "docker";

    public int ProbeTimeoutSeconds { get; set; } = 8;
}
