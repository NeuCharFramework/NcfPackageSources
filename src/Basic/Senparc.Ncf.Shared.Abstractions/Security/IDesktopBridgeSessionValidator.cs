/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: IDesktopBridgeSessionValidator.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.7.0-preview1 implement connection manager and workspace file system provider

----------------------------------------------------------------*/

using System.Threading;

namespace Senparc.Ncf.Shared.Abstractions.Security;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public interface IDesktopBridgeSessionValidator
{
    string HeaderName { get; }

    bool IsConfigured { get; }

    bool TryAuthorize(string? suppliedToken, out CancellationToken sessionRevoked);
}
