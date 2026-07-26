/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeTokenValidator.cs
    文件功能描述：DesktopBridge 本机会话令牌校验

    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v0.1.0-preview2 同步模块功能与兼容性改进

----------------------------------------------------------------*/

using System.Security.Cryptography;
using System.Text;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopBridgeTokenValidator
{
    public const string TokenEnvironmentVariable = "NCF_DESKTOP_BRIDGE_TOKEN";
    public const string TokenHeaderName = "X-Ncf-Desktop-Token";

    private readonly byte[]? _expectedToken;

    public DesktopBridgeTokenValidator()
    {
        var configuredToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredToken))
        {
            _expectedToken = Encoding.UTF8.GetBytes(configuredToken);
        }
    }

    public bool IsConfigured => _expectedToken is { Length: > 0 };

    public bool IsAuthorized(string? suppliedToken)
    {
        if (!IsConfigured || string.IsNullOrEmpty(suppliedToken))
        {
            return false;
        }

        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedToken);
        return suppliedBytes.Length == _expectedToken!.Length &&
               CryptographicOperations.FixedTimeEquals(suppliedBytes, _expectedToken);
    }
}
