/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IDesktopBridgeSessionValidator.cs
    文件功能描述：桌面桥接会话验证公共契约

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Threading;

namespace Senparc.Ncf.Shared.Abstractions.Security;

/// <summary>
/// 供独立 XNCF 模块复用 DesktopBridge 设备会话，而不依赖 DesktopBridge 实现程序集。
/// </summary>
public interface IDesktopBridgeSessionValidator
{
    string HeaderName { get; }

    bool IsConfigured { get; }

    bool TryAuthorize(string? suppliedToken, out CancellationToken sessionRevoked);
}
