namespace Senparc.Xncf.Sandbox.Abstractions;

/// <summary>
/// 沙箱会话状态。
/// </summary>
public enum SandboxSessionStatus
{
    Creating = 0,
    Running = 1,
    Stopping = 2,
    Stopped = 3,
    Failed = 4,
    Expired = 5
}
