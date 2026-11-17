/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopBridgeTokenValidator.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.1.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.4.8 implement connection manager and workspace file system provider

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Security;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopBridgeTokenValidator : IDesktopBridgeSessionValidator
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

    public string HeaderName => TokenHeaderName;

    public bool IsAuthorized(string? suppliedToken) => _credentialStore.IsAuthorized(suppliedToken);

    public bool TryAuthorize(string? suppliedToken, out CancellationToken sessionRevoked) =>
        _credentialStore.TryAuthorize(suppliedToken, out sessionRevoked);
}
