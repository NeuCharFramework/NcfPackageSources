/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopAdminAuthHandoffStore.cs
    File description: Related implementation.

    Created by: Senparc - 20260804

    Modified by: Senparc - 20260808
    Change description: v0.4.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Senparc.Xncf.DesktopBridge.Models;

namespace Senparc.Xncf.DesktopBridge.Services;

/// <summary>
/// Stores short-lived PKCE-protected desktop-to-admin authorization handoffs.
/// </summary>
public sealed class DesktopAdminAuthHandoffStore
{
    public static readonly TimeSpan HandoffLifetime = TimeSpan.FromSeconds(60);
    public const int PollIntervalMilliseconds = 750;

    private const int MaxPendingHandoffs = 100;
    private const int MaxPendingPerDesktopSession = 3;
    private readonly object _gate = new();
    private readonly Dictionary<Guid, PendingHandoff> _handoffs = new();

    /// <summary>
    /// Creates a bounded authorization handoff request for a desktop session.
    /// </summary>
    /// <param name="desktopSessionToken">The desktop session token to bind to the handoff.</param>
    /// <param name="codeChallenge">The base64url SHA-256 PKCE code challenge.</param>
    /// <param name="returnPath">The validated local admin return path.</param>
    /// <returns>The request ID, approval path, expiration, and polling interval.</returns>
    /// <exception cref="DesktopAdminAuthHandoffException">Thrown when the session or PKCE challenge is invalid.</exception>
    /// <exception cref="DesktopAdminAuthHandoffRateLimitException">Thrown when pending handoff limits are exceeded.</exception>
    public DesktopAdminAuthHandoffCreateResponse Create(
        string desktopSessionToken,
        string? codeChallenge,
        string? returnPath)
    {
        if (string.IsNullOrWhiteSpace(desktopSessionToken))
        {
            throw new DesktopAdminAuthHandoffException("The desktop session is invalid.");
        }

        if (!TryDecodeCodeChallenge(codeChallenge, out var challengeBytes))
        {
            throw new DesktopAdminAuthHandoffException("The PKCE challenge is invalid.");
        }

        var now = DateTimeOffset.UtcNow;
        var sessionTokenHash = Hash(desktopSessionToken);
        lock (_gate)
        {
            CleanupExpiredLocked(now);
            if (_handoffs.Count >= MaxPendingHandoffs ||
                _handoffs.Values.Count(handoff =>
                    CryptographicOperations.FixedTimeEquals(handoff.DesktopSessionTokenHash, sessionTokenHash)) >=
                MaxPendingPerDesktopSession)
            {
                throw new DesktopAdminAuthHandoffRateLimitException();
            }

            var requestId = Guid.NewGuid();
            var expiresAt = now.Add(HandoffLifetime);
            var safeReturnPath = NormalizeReturnPath(returnPath);
            _handoffs[requestId] = new PendingHandoff(
                requestId,
                sessionTokenHash,
                challengeBytes,
                safeReturnPath,
                expiresAt);

            var approvalPath = QueryHelpers.AddQueryString(
                "/Admin/DesktopBridge/AuthHandoff",
                new Dictionary<string, string?>
                {
                    ["requestId"] = requestId.ToString("D"),
                    ["returnUrl"] = safeReturnPath
                });
            return new DesktopAdminAuthHandoffCreateResponse(
                requestId,
                expiresAt,
                approvalPath,
                PollIntervalMilliseconds);
        }
    }

    /// <summary>
    /// Determines whether a handoff request is still awaiting administrator approval.
    /// </summary>
    /// <param name="requestId">The handoff request identifier.</param>
    /// <returns><see langword="true"/> when the request is pending and unexpired.</returns>
    public bool IsPending(Guid requestId)
    {
        lock (_gate)
        {
            CleanupExpiredLocked(DateTimeOffset.UtcNow);
            return _handoffs.TryGetValue(requestId, out var handoff) &&
                   handoff.Status == HandoffStatus.Pending;
        }
    }

    /// <summary>
    /// Approves a pending handoff after validating administrator identity and source-session lifetime.
    /// </summary>
    /// <param name="requestId">The handoff request identifier.</param>
    /// <param name="adminUserId">The authenticated administrator identifier.</param>
    /// <param name="userName">The administrator display name.</param>
    /// <param name="sourceAuthenticationExpiresUtc">The source authentication expiration time.</param>
    /// <returns><see langword="true"/> when the pending request was approved.</returns>
    public bool Approve(
        Guid requestId,
        int adminUserId,
        string userName,
        DateTimeOffset sourceAuthenticationExpiresUtc)
    {
        var now = DateTimeOffset.UtcNow;
        if (adminUserId <= 0 || string.IsNullOrWhiteSpace(userName) ||
            sourceAuthenticationExpiresUtc <= now.AddSeconds(10))
        {
            return false;
        }

        lock (_gate)
        {
            CleanupExpiredLocked(now);
            if (!_handoffs.TryGetValue(requestId, out var handoff) ||
                handoff.Status != HandoffStatus.Pending)
            {
                return false;
            }

            handoff.Status = HandoffStatus.Approved;
            handoff.AdminUserId = adminUserId;
            handoff.UserName = userName.Trim();
            handoff.SourceAuthenticationExpiresUtc = sourceAuthenticationExpiresUtc;
            return true;
        }
    }

    /// <summary>
    /// Denies a pending handoff request with a bounded message.
    /// </summary>
    /// <param name="requestId">The handoff request identifier.</param>
    /// <param name="message">The reason shown to the desktop client.</param>
    /// <returns><see langword="true"/> when a pending request was denied.</returns>
    public bool Deny(Guid requestId, string message)
    {
        lock (_gate)
        {
            CleanupExpiredLocked(DateTimeOffset.UtcNow);
            if (!_handoffs.TryGetValue(requestId, out var handoff) ||
                handoff.Status != HandoffStatus.Pending)
            {
                return false;
            }

            handoff.Status = HandoffStatus.Denied;
            handoff.Message = string.IsNullOrWhiteSpace(message)
                ? "WebView login cannot be used for desktop authorization."
                : message.Trim();
            return true;
        }
    }

    /// <summary>
    /// Redeems an approved handoff after validating the desktop session token and PKCE verifier.
    /// </summary>
    /// <param name="requestId">The handoff request identifier.</param>
    /// <param name="desktopSessionToken">The desktop session token bound at creation.</param>
    /// <param name="codeVerifier">The PKCE verifier matching the stored challenge.</param>
    /// <returns>Pending, invalid, denied, or approved identity data.</returns>
    public DesktopAdminAuthHandoffRedeemResult Redeem(
        Guid requestId,
        string desktopSessionToken,
        string? codeVerifier)
    {
        if (requestId == Guid.Empty || string.IsNullOrWhiteSpace(desktopSessionToken) ||
            string.IsNullOrWhiteSpace(codeVerifier) || codeVerifier.Length > 128)
        {
            return DesktopAdminAuthHandoffRedeemResult.Invalid;
        }

        var now = DateTimeOffset.UtcNow;
        var suppliedSessionHash = Hash(desktopSessionToken);
        var suppliedChallenge = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        lock (_gate)
        {
            CleanupExpiredLocked(now);
            if (!_handoffs.TryGetValue(requestId, out var handoff) ||
                !CryptographicOperations.FixedTimeEquals(
                    suppliedSessionHash,
                    handoff.DesktopSessionTokenHash) ||
                !CryptographicOperations.FixedTimeEquals(suppliedChallenge, handoff.CodeChallenge))
            {
                return DesktopAdminAuthHandoffRedeemResult.Invalid;
            }

            if (handoff.Status == HandoffStatus.Pending)
            {
                return DesktopAdminAuthHandoffRedeemResult.Pending;
            }

            _handoffs.Remove(requestId);
            if (handoff.Status == HandoffStatus.Denied)
            {
                return new DesktopAdminAuthHandoffRedeemResult("denied", Message: handoff.Message);
            }

            if (handoff.AdminUserId <= 0 || string.IsNullOrWhiteSpace(handoff.UserName) ||
                handoff.SourceAuthenticationExpiresUtc is not { } sourceExpiresUtc || sourceExpiresUtc <= now)
            {
                return DesktopAdminAuthHandoffRedeemResult.Invalid;
            }

            return new DesktopAdminAuthHandoffRedeemResult(
                "approved",
                handoff.AdminUserId,
                handoff.UserName,
                sourceExpiresUtc);
        }
    }

    /// <summary>
    /// Removes expired handoff requests while the store lock is held.
    /// </summary>
    /// <param name="now">The current UTC timestamp used for expiry comparison.</param>
    private void CleanupExpiredLocked(DateTimeOffset now)
    {
        foreach (var requestId in _handoffs
                     .Where(pair => pair.Value.ExpiresAt <= now)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _handoffs.Remove(requestId);
        }
    }

    /// <summary>
    /// Decodes and validates a base64url PKCE code challenge.
    /// </summary>
    /// <param name="value">The encoded challenge.</param>
    /// <param name="bytes">Receives the 32-byte challenge.</param>
    /// <returns><see langword="true"/> when the challenge is valid.</returns>
    private static bool TryDecodeCodeChallenge(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(value) || value.Length != 43)
        {
            return false;
        }

        try
        {
            bytes = WebEncoders.Base64UrlDecode(value);
            return bytes.Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Restricts a requested return path to local Admin routes and bounds its length.
    /// </summary>
    /// <param name="returnPath">The requested return path.</param>
    /// <returns>A safe local Admin path.</returns>
    private static string NormalizeReturnPath(string? returnPath)
    {
        if (string.IsNullOrWhiteSpace(returnPath) ||
            !returnPath.StartsWith("/", StringComparison.Ordinal) ||
            returnPath.StartsWith("//", StringComparison.Ordinal) ||
            !returnPath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(returnPath, UriKind.Relative, out _))
        {
            return "/Admin/Index";
        }

        var normalized = returnPath.Length <= 1024 ? returnPath : returnPath[..1024];
        return normalized.StartsWith("/Admin/DesktopBridge/AuthHandoff", StringComparison.OrdinalIgnoreCase)
            ? "/Admin/Index"
            : normalized;
    }

    /// <summary>
    /// Hashes a desktop/session value for comparison without retaining plaintext.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>The SHA-256 digest.</returns>
    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private enum HandoffStatus
    {
        Pending,
        Approved,
        Denied
    }

    private sealed class PendingHandoff
    {
        /// <summary>
        /// Initializes a pending administrator-authentication handoff.
        /// </summary>
        /// <param name="requestId">The handoff request identifier.</param>
        /// <param name="desktopSessionTokenHash">The desktop session token digest.</param>
        /// <param name="codeChallenge">The PKCE code challenge.</param>
        /// <param name="returnPath">The normalized return path.</param>
        /// <param name="expiresAt">The expiration timestamp.</param>
        public PendingHandoff(
            Guid requestId,
            byte[] desktopSessionTokenHash,
            byte[] codeChallenge,
            string returnPath,
            DateTimeOffset expiresAt)
        {
            RequestId = requestId;
            DesktopSessionTokenHash = desktopSessionTokenHash;
            CodeChallenge = codeChallenge;
            ReturnPath = returnPath;
            ExpiresAt = expiresAt;
        }

        public Guid RequestId { get; }
        public byte[] DesktopSessionTokenHash { get; }
        public byte[] CodeChallenge { get; }
        public string ReturnPath { get; }
        public DateTimeOffset ExpiresAt { get; }
        public HandoffStatus Status { get; set; }
        public int AdminUserId { get; set; }
        public string? UserName { get; set; }
        public DateTimeOffset? SourceAuthenticationExpiresUtc { get; set; }
        public string? Message { get; set; }
    }
}

public sealed record DesktopAdminAuthHandoffRedeemResult(
    string Status,
    int? AdminUserId = null,
    string? UserName = null,
    DateTimeOffset? SourceAuthenticationExpiresUtc = null,
    string? Message = null)
{
    public static DesktopAdminAuthHandoffRedeemResult Pending { get; } = new("pending");
    public static DesktopAdminAuthHandoffRedeemResult Invalid { get; } = new("invalid");
}

public class DesktopAdminAuthHandoffException : Exception
{
    /// <summary>
    /// Initializes an administrator-authentication handoff exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DesktopAdminAuthHandoffException(string message) : base(message)
    {
    }
}

public sealed class DesktopAdminAuthHandoffRateLimitException : DesktopAdminAuthHandoffException
{
    /// <summary>
    /// Initializes the standard handoff rate-limit exception.
    /// </summary>
    public DesktopAdminAuthHandoffRateLimitException()
        : base("Desktop login authorization requests are being sent too frequently. Try again later.")
    {
    }
}
