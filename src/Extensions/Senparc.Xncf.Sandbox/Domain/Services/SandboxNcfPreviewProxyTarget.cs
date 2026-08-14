/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxNcfPreviewProxyTarget.cs
    文件功能描述：NCF 预览反向代理目标数据


    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.2.0-preview3 增加 NCF 预览沙箱工作负载

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>Internal-only target data for the NCF preview reverse proxy.</summary>
public sealed class SandboxNcfPreviewProxyTarget
{
    public required string SessionId { get; init; }
    public required int HostPort { get; init; }
}
