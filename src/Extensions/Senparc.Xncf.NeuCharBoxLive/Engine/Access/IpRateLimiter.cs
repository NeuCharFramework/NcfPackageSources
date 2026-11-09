/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IpRateLimiter.cs
    文件功能描述：NeuCharBox Live 引擎（Access）：IpRateLimiter


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>
/// At most <see cref="Limit"/> hits per IP in any sliding <see cref="Window"/>. Refusals don't count.
/// </summary>
/// <remarks>
/// Memory is bounded: at most <see cref="MaxTracked"/> IPs are tracked. A new IP that finds the map full first drops the
/// IPs whose hits have all left the window; if none have, it is refused. Under a flood from that many addresses,
/// refusing newcomers protects the store, and it lasts only until the flood's hits leave the window.
/// </remarks>
public class IpRateLimiter(int limit, TimeSpan window, TimeProvider tp, int maxTracked = IpRateLimiter.DefaultMaxTracked)
{
    /// <summary>About 4,000 addresses: a few MB at most for a 30-a-minute limiter.</summary>
    public const int DefaultMaxTracked = 4096;

    readonly object _gate = new();
    readonly Dictionary<string, Queue<DateTimeOffset>> _hits = new(StringComparer.Ordinal);

    public int Limit { get; } = limit;
    public TimeSpan Window { get; } = window;
    public int MaxTracked { get; } = maxTracked;

    /// <summary>IPs in the map now.</summary>
    public int Tracked { get { lock (_gate) return _hits.Count; } }

    public bool TryAcquire(string ip)
    {
        ip ??= "";
        lock (_gate)
        {
            var now = tp.GetUtcNow();
            if (!_hits.TryGetValue(ip, out var q))
            {
                if (_hits.Count >= MaxTracked && Prune(now) == 0) return false;
                _hits[ip] = q = new Queue<DateTimeOffset>();
            }
            Expire(q, now);
            if (q.Count >= Limit) return false;
            q.Enqueue(now);
            return true;
        }
    }

    void Expire(Queue<DateTimeOffset> q, DateTimeOffset now)
    {
        while (q.Count > 0 && now - q.Peek() >= Window) q.Dequeue();
    }

    /// <summary>Drops the IPs whose hits have all left the window; returns how many went.</summary>
    int Prune(DateTimeOffset now)
    {
        List<string>? gone = null;
        foreach (var (ip, q) in _hits)
        {
            Expire(q, now);
            if (q.Count == 0) (gone ??= new()).Add(ip);
        }
        foreach (var ip in gone ?? Enumerable.Empty<string>()) _hits.Remove(ip);
        return gone?.Count ?? 0;
    }
}

/// <summary>Applications: at most <see cref="PerHour"/> per IP in any sliding hour (global constraints).</summary>
public sealed class ApplyRateLimiter(TimeProvider tp) : IpRateLimiter(PerHour, TimeSpan.FromHours(1), tp)
{
    public const int PerHour = 5;
}

/// <summary>Hand edits and clock advances: at most 10 per session (the key is the session id) in any sliding 5 s.</summary>
public sealed class EditRateLimiter(TimeProvider tp) : IpRateLimiter(10, TimeSpan.FromSeconds(5), tp);

/// <summary>Invite-code checks: at most <see cref="LiveOptions.AccessCheckPerMinute"/> per IP in any sliding minute.</summary>
public sealed class AccessCheckRateLimiter(LiveOptions opt, TimeProvider tp)
    : IpRateLimiter(opt.AccessCheckPerMinute, TimeSpan.FromMinutes(1), tp);
