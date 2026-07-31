/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeTokenValidator.cs
    文件功能描述：DesktopBridge 本机会话令牌校验

    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v0.1.0-preview2 同步模块功能与兼容性改进

----------------------------------------------------------------*/

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopBridgeTokenValidator
{
    public const string TokenEnvironmentVariable = "NCF_DESKTOP_BRIDGE_TOKEN";
    public const string TokenHeaderName = "X-Ncf-Desktop-Token";

    private readonly DesktopBridgeCredentialStore _credentialStore;

    public DesktopBridgeTokenValidator()
        : this(new DesktopBridgeCredentialStore())
    {
    }

    public DesktopBridgeTokenValidator(DesktopBridgeCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public bool IsConfigured => _credentialStore.IsConfigured;

    public bool IsAuthorized(string? suppliedToken) => _credentialStore.IsAuthorized(suppliedToken);
}
