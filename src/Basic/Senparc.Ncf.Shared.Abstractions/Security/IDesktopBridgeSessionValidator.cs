/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IDesktopBridgeSessionValidator.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.7.0-preview1 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.7.1.2-preview3 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.Threading;

namespace Senparc.Ncf.Shared.Abstractions.Security;

/// <summary>
/// Validates the session token used by DesktopBridge connections.
/// </summary>
public interface IDesktopBridgeSessionValidator
{
    /// <summary>
    /// Gets the HTTP header name from which the session token is read.
    /// </summary>
    string HeaderName { get; }

    /// <summary>
    /// Gets a value indicating whether DesktopBridge token validation is configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Validates a supplied token and returns the revocation signal for the session.
    /// </summary>
    /// <param name="suppliedToken">The token supplied by the desktop client.</param>
    /// <param name="sessionRevoked">Receives a token that is cancelled when the session is revoked.</param>
    /// <returns><see langword="true"/> when the token is authorized.</returns>
    bool TryAuthorize(string? suppliedToken, out CancellationToken sessionRevoked);
}
