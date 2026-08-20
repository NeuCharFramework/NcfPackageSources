/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopAdminAuthHandoffStore.cs
    文件功能描述：短期、单次、绑定 DesktopBridge 会话的管理员换票状态

    创建标识：Senparc - 20260804

    修改标识：Senparc - 20260808
    修改描述：v0.4.0-preview4 新增单次绑定会话的管理员换票状态存储

----------------------------------------------------------------*/

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Senparc.Xncf.DesktopBridge.Models;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopAdminAuthHandoffStore
{
    public static readonly TimeSpan HandoffLifetime = TimeSpan.FromSeconds(60);
    public const int PollIntervalMilliseconds = 750;

    private const int MaxPendingHandoffs = 100;
    private const int MaxPendingPerDesktopSession = 3;
    private readonly object _gate = new();
    private readonly Dictionary<Guid, PendingHandoff> _handoffs = new();

    public DesktopAdminAuthHandoffCreateResponse Create(
        string desktopSessionToken,
        string? codeChallenge,
        string? returnPath)
    {
        if (string.IsNullOrWhiteSpace(desktopSessionToken))
        {
            throw new DesktopAdminAuthHandoffException(DesktopBridgeResource.Get("Auth.Error.InvalidDesktopSession"));
        }

        if (!TryDecodeCodeChallenge(codeChallenge, out var challengeBytes))
        {
            throw new DesktopAdminAuthHandoffException(DesktopBridgeResource.Get("Auth.Error.InvalidPkce"));
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

    public bool IsPending(Guid requestId)
    {
        lock (_gate)
        {
            CleanupExpiredLocked(DateTimeOffset.UtcNow);
            return _handoffs.TryGetValue(requestId, out var handoff) &&
                   handoff.Status == HandoffStatus.Pending;
        }
    }

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
                ? DesktopBridgeResource.Get("Auth.Error.WebViewDenied")
                : message.Trim();
            return true;
        }
    }

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

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private enum HandoffStatus
    {
        Pending,
        Approved,
        Denied
    }

    private sealed class PendingHandoff
    {
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
    public DesktopAdminAuthHandoffException(string message) : base(message)
    {
    }
}

public sealed class DesktopAdminAuthHandoffRateLimitException : DesktopAdminAuthHandoffException
{
    public DesktopAdminAuthHandoffRateLimitException()
        : base(DesktopBridgeResource.Get("Auth.Error.RateLimit.Message"))
    {
    }
}
