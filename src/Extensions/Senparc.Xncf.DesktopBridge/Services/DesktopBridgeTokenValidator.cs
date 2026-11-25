/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopBridgeTokenValidator.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.1.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.4.8 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Security;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopBridgeTokenValidator : IDesktopBridgeSessionValidator
{
    public const string TokenEnvironmentVariable = "NCF_DESKTOP_BRIDGE_TOKEN";
    public const string TokenHeaderName = "X-Ncf-Desktop-Token";

    private readonly DesktopBridgeCredentialStore _credentialStore;

    /// <summary>
    /// Initializes a validator using the default credential store.
    /// </summary>
    public DesktopBridgeTokenValidator()
        : this(new DesktopBridgeCredentialStore())
    {
    }

    /// <summary>
    /// Initializes a validator over an explicit credential store.
    /// </summary>
    /// <param name="credentialStore">The store used to validate legacy and paired session tokens.</param>
    public DesktopBridgeTokenValidator(DesktopBridgeCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public bool IsConfigured => _credentialStore.IsConfigured;

    public string HeaderName => TokenHeaderName;

    /// <summary>
    /// Determines whether a supplied desktop token is authorized.
    /// </summary>
    /// <param name="suppliedToken">The token supplied by the desktop client.</param>
    /// <returns><see langword="true"/> when the token is valid.</returns>
    public bool IsAuthorized(string? suppliedToken) => _credentialStore.IsAuthorized(suppliedToken);

    /// <summary>
    /// Validates a desktop token and returns its paired-session revocation token.
    /// </summary>
    /// <param name="suppliedToken">The token supplied by the desktop client.</param>
    /// <param name="sessionRevoked">Receives a token canceled when the paired session is revoked.</param>
    /// <returns><see langword="true"/> when authorization succeeds.</returns>
    public bool TryAuthorize(string? suppliedToken, out CancellationToken sessionRevoked) =>
        _credentialStore.TryAuthorize(suppliedToken, out sessionRevoked);
}
