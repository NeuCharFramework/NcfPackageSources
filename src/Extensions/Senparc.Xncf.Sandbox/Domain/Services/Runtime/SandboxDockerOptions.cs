/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxDockerOptions.cs
    文件功能描述：Docker 沙箱运行参数

    创建标识：Senparc - 20260815

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// 配置节：SenparcXncfSandbox:Docker
/// </summary>
public sealed class SandboxDockerOptions
{
    public const string SectionName = "SenparcXncfSandbox:Docker";

    /// <summary>
    /// Jupyter 交互式容器的完整 docker run 超时时间。
    /// 当本地没有镜像时，docker run 会在该时间内同步拉取镜像。
    /// </summary>
    public int InteractiveCreateTimeoutSeconds { get; set; } = 900;

    /// <summary>
    /// 将配置限制在 1 分钟至 1 小时，避免错误配置导致请求无限等待。
    /// </summary>
    public TimeSpan GetInteractiveCreateTimeout() =>
        TimeSpan.FromSeconds(Math.Clamp(InteractiveCreateTimeoutSeconds, 60, 3600));
}
