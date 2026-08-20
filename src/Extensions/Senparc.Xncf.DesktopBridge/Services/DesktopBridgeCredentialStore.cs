/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeCredentialStore.cs
    文件功能描述：DesktopBridge 配对请求和短期会话凭据存储

    创建标识：Senparc - 20260801

    修改标识：Senparc - 20260804
    修改描述：v0.3.0-preview3 新增桌面端同步提供程序

    修改标识：Senparc - 20260804
    修改描述：v0.3.0-preview3 将同步提供程序统一更名为 NeuBell/纽铃

----------------------------------------------------------------*/

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.DesktopBridge.Models;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopBridgeCredentialStore
{
    public static readonly TimeSpan PairingLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(24);
    public const int PollIntervalSeconds = 2;

    private const int MaxPendingPerAddress = 3;
    private const int MaxPendingRequests = 100;
    private const int MaxActiveSessions = 100;
    private const string DeviceCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly object _gate = new();
    private readonly byte[]? _legacyTokenHash;
    private readonly INeuBellPublisher? _neuBellPublisher;
    private readonly Dictionary<Guid, PendingPairing> _pendingPairings = new();
    private readonly Dictionary<Guid, SessionCredential> _sessions = new();

    public DesktopBridgeCredentialStore(INeuBellPublisher? neuBellPublisher = null)
        : this(Environment.GetEnvironmentVariable(DesktopBridgeTokenValidator.TokenEnvironmentVariable), neuBellPublisher)
    {
    }

    internal DesktopBridgeCredentialStore(string? legacyToken, INeuBellPublisher? neuBellPublisher = null)
    {
        _neuBellPublisher = neuBellPublisher;
        if (!string.IsNullOrWhiteSpace(legacyToken))
        {
            _legacyTokenHash = HashToken(legacyToken);
        }
    }

    public bool IsConfigured
    {
        get
        {
            lock (_gate)
            {
                CleanupExpiredLocked(DateTimeOffset.UtcNow);
                return _legacyTokenHash is { Length: > 0 } || _sessions.Count > 0;
            }
        }
    }

    public DesktopBridgePairingCreateResponse CreatePairingRequest(
        string? clientName,
        string? remoteAddress)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedAddress = Normalize(remoteAddress, DesktopBridgeResource.Get("Pairing.UnknownAddress"), 128);
        var normalizedClientName = Normalize(clientName, "NcfDesktopApp GUI", 80);

        lock (_gate)
        {
            CleanupExpiredLocked(now);
            var pendingForAddress = _pendingPairings.Values.Count(pairing =>
                pairing.Status == PairingStatus.Pending &&
                string.Equals(pairing.RemoteAddress, normalizedAddress, StringComparison.Ordinal));
            if (pendingForAddress >= MaxPendingPerAddress || _pendingPairings.Count >= MaxPendingRequests)
            {
                throw new DesktopBridgePairingRateLimitException();
            }

            var requestId = Guid.NewGuid();
            var pollSecret = CreateToken();
            var deviceCode = CreateUniqueDeviceCodeLocked();
            var expiresAt = now.Add(PairingLifetime);
            _pendingPairings[requestId] = new PendingPairing(
                requestId,
                deviceCode,
                normalizedClientName,
                normalizedAddress,
                HashToken(pollSecret),
                now,
                expiresAt);
            NotifyNeuBellChanged();

            return new DesktopBridgePairingCreateResponse(
                requestId,
                deviceCode,
                pollSecret,
                expiresAt,
                $"/Admin/DesktopBridge/Index?uid={Register.ModuleUid}",
                PollIntervalSeconds);
        }
    }

    public DesktopBridgePairingPollResult Poll(Guid requestId, string? pollSecret)
    {
        if (requestId == Guid.Empty || string.IsNullOrWhiteSpace(pollSecret))
        {
            return DesktopBridgePairingPollResult.Invalid;
        }

        var now = DateTimeOffset.UtcNow;
        var suppliedSecretHash = HashToken(pollSecret);
        lock (_gate)
        {
            if (!_pendingPairings.TryGetValue(requestId, out var pairing) ||
                !CryptographicOperations.FixedTimeEquals(suppliedSecretHash, pairing.PollSecretHash))
            {
                CleanupExpiredLocked(now);
                return DesktopBridgePairingPollResult.Invalid;
            }

            if (pairing.ExpiresAt <= now)
            {
                _pendingPairings.Remove(requestId);
                CleanupExpiredLocked(now);
                return new DesktopBridgePairingPollResult(
                    "expired",
                    null,
                    null,
                    DesktopBridgeResource.Get("Pairing.Expired"));
            }

            CleanupExpiredLocked(now);
            return pairing.Status switch
            {
                PairingStatus.Pending => new DesktopBridgePairingPollResult("pending"),
                PairingStatus.Denied => new DesktopBridgePairingPollResult(
                    "denied",
                    null,
                    null,
                    DesktopBridgeResource.Get("Pairing.Denied")),
                PairingStatus.Approved when !string.IsNullOrEmpty(pairing.SessionTokenDelivery) =>
                    new DesktopBridgePairingPollResult(
                        "approved",
                        pairing.SessionTokenDelivery,
                        pairing.SessionExpiresAt),
                _ => DesktopBridgePairingPollResult.Invalid
            };
        }
    }

    public bool Approve(Guid requestId, string? approvedBy)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            CleanupExpiredLocked(now);
            if (!_pendingPairings.TryGetValue(requestId, out var pairing) ||
                pairing.Status != PairingStatus.Pending ||
                pairing.ExpiresAt <= now ||
                _sessions.Count >= MaxActiveSessions)
            {
                return false;
            }

            var sessionId = Guid.NewGuid();
            var sessionToken = CreateToken();
            var sessionExpiresAt = now.Add(SessionLifetime);
            _sessions[sessionId] = new SessionCredential(
                sessionId,
                pairing.ClientName,
                pairing.RemoteAddress,
                Normalize(approvedBy, DesktopBridgeResource.Get("Pairing.DefaultAdministrator"), 80),
                HashToken(sessionToken),
                now,
                sessionExpiresAt);

            pairing.Status = PairingStatus.Approved;
            pairing.SessionId = sessionId;
            pairing.SessionTokenDelivery = sessionToken;
            pairing.SessionExpiresAt = sessionExpiresAt;
            NotifyNeuBellChanged();
            return true;
        }
    }

    public bool Deny(Guid requestId)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            CleanupExpiredLocked(now);
            if (!_pendingPairings.TryGetValue(requestId, out var pairing) ||
                pairing.Status != PairingStatus.Pending)
            {
                return false;
            }

            pairing.Status = PairingStatus.Denied;
            NotifyNeuBellChanged();
            return true;
        }
    }

    public bool Revoke(Guid sessionId)
    {
        lock (_gate)
        {
            if (!_sessions.Remove(sessionId, out var session))
            {
                return false;
            }

            session.RevocationSource.Cancel(throwOnFirstException: false);

            foreach (var pairing in _pendingPairings.Values.Where(x => x.SessionId == sessionId))
            {
                pairing.Status = PairingStatus.Denied;
                pairing.SessionTokenDelivery = null;
                pairing.SessionExpiresAt = null;
            }

            return true;
        }
    }

    private void NotifyNeuBellChanged()
    {
        if (_neuBellPublisher == null)
        {
            return;
        }

        var notification = _neuBellPublisher.NotifyChangedAsync(DesktopBridgeNeuBellProvider.ProviderIdValue);
        if (!notification.IsCompletedSuccessfully)
        {
            _ = notification.AsTask();
        }
    }

    public IReadOnlyList<DesktopBridgePendingPairingView> GetPendingPairings()
    {
        lock (_gate)
        {
            CleanupExpiredLocked(DateTimeOffset.UtcNow);
            return _pendingPairings.Values
                .Where(x => x.Status == PairingStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new DesktopBridgePendingPairingView(
                    x.RequestId,
                    x.DeviceCode,
                    x.ClientName,
                    x.RemoteAddress,
                    x.CreatedAt,
                    x.ExpiresAt))
                .ToArray();
        }
    }

    public IReadOnlyList<DesktopBridgeSessionView> GetSessions()
    {
        lock (_gate)
        {
            CleanupExpiredLocked(DateTimeOffset.UtcNow);
            return _sessions.Values
                .OrderByDescending(x => x.ApprovedAt)
                .Select(x => new DesktopBridgeSessionView(
                    x.SessionId,
                    x.ClientName,
                    x.RemoteAddress,
                    x.ApprovedBy,
                    x.ApprovedAt,
                    x.ExpiresAt,
                    x.LastUsedAt))
                .ToArray();
        }
    }

    public bool IsAuthorized(string? suppliedToken)
    {
        return TryAuthorize(suppliedToken, out _);
    }

    public bool TryAuthorize(string? suppliedToken, out CancellationToken sessionRevoked)
    {
        sessionRevoked = CancellationToken.None;
        if (string.IsNullOrWhiteSpace(suppliedToken))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var suppliedHash = HashToken(suppliedToken);
        lock (_gate)
        {
            CleanupExpiredLocked(now);
            if (_legacyTokenHash is { Length: > 0 } &&
                CryptographicOperations.FixedTimeEquals(suppliedHash, _legacyTokenHash))
            {
                return true;
            }

            foreach (var session in _sessions.Values)
            {
                if (!CryptographicOperations.FixedTimeEquals(suppliedHash, session.TokenHash))
                {
                    continue;
                }

                session.LastUsedAt = now;
                sessionRevoked = session.RevocationSource.Token;
                return true;
            }

            return false;
        }
    }

    private void CleanupExpiredLocked(DateTimeOffset now)
    {
        foreach (var requestId in _pendingPairings
                     .Where(x => x.Value.ExpiresAt <= now)
                     .Select(x => x.Key)
                     .ToArray())
        {
            _pendingPairings.Remove(requestId);
        }

        foreach (var sessionId in _sessions
                     .Where(x => x.Value.ExpiresAt <= now)
                     .Select(x => x.Key)
                     .ToArray())
        {
            if (_sessions.Remove(sessionId, out var session))
            {
                session.RevocationSource.Cancel(throwOnFirstException: false);
            }
        }
    }

    private string CreateUniqueDeviceCodeLocked()
    {
        Span<char> characters = stackalloc char[12];
        string code;
        do
        {
            for (var i = 0; i < characters.Length; i++)
            {
                characters[i] = DeviceCodeAlphabet[RandomNumberGenerator.GetInt32(DeviceCodeAlphabet.Length)];
            }

            code = $"{characters[..4]}-{characters[4..8]}-{characters[8..]}";
        }
        while (_pendingPairings.Values.Any(x => string.Equals(x.DeviceCode, code, StringComparison.Ordinal)));

        return code;
    }

    private static string CreateToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    private static string Normalize(string? value, string fallback, int maximumLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private enum PairingStatus
    {
        Pending,
        Approved,
        Denied
    }

    private sealed class PendingPairing
    {
        public PendingPairing(
            Guid requestId,
            string deviceCode,
            string clientName,
            string remoteAddress,
            byte[] pollSecretHash,
            DateTimeOffset createdAt,
            DateTimeOffset expiresAt)
        {
            RequestId = requestId;
            DeviceCode = deviceCode;
            ClientName = clientName;
            RemoteAddress = remoteAddress;
            PollSecretHash = pollSecretHash;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }

        public Guid RequestId { get; }
        public string DeviceCode { get; }
        public string ClientName { get; }
        public string RemoteAddress { get; }
        public byte[] PollSecretHash { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset ExpiresAt { get; }
        public PairingStatus Status { get; set; }
        public Guid? SessionId { get; set; }
        public string? SessionTokenDelivery { get; set; }
        public DateTimeOffset? SessionExpiresAt { get; set; }
    }

    private sealed class SessionCredential
    {
        public SessionCredential(
            Guid sessionId,
            string clientName,
            string remoteAddress,
            string approvedBy,
            byte[] tokenHash,
            DateTimeOffset approvedAt,
            DateTimeOffset expiresAt)
        {
            SessionId = sessionId;
            ClientName = clientName;
            RemoteAddress = remoteAddress;
            ApprovedBy = approvedBy;
            TokenHash = tokenHash;
            ApprovedAt = approvedAt;
            ExpiresAt = expiresAt;
        }

        public Guid SessionId { get; }
        public string ClientName { get; }
        public string RemoteAddress { get; }
        public string ApprovedBy { get; }
        public byte[] TokenHash { get; }
        public DateTimeOffset ApprovedAt { get; }
        public DateTimeOffset ExpiresAt { get; }
        public DateTimeOffset? LastUsedAt { get; set; }
        public CancellationTokenSource RevocationSource { get; } = new();
    }
}

public sealed record DesktopBridgePairingPollResult(
    string Status,
    string? SessionToken = null,
    DateTimeOffset? SessionExpiresAt = null,
    string? Message = null)
{
    public static DesktopBridgePairingPollResult Invalid { get; } = new("invalid");
}

public sealed class DesktopBridgePairingRateLimitException : Exception
{
    public DesktopBridgePairingRateLimitException()
        : base(DesktopBridgeResource.Get("Pairing.RateLimit.Message"))
    {
    }
}
