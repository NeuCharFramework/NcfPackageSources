/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：TokenMeter.cs
    文件功能描述：NeuCharBox Live 引擎（Access）：TokenMeter


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>
/// The daily token kill switch (spec 4.4). A run's tokens reach the store only when it finishes, so the meter keeps two
/// counts for the day: the store's (re-read at most every <see cref="ReadEvery"/>), and the store's at the day's first
/// read plus every token added since. Spent once the larger reaches <see cref="LiveOptions.DailyTokenBudget"/>: the
/// second sees runs still in flight, and taking the larger never counts a finished run twice.
/// </summary>
/// <remarks>
/// Fails closed: a store that can't be read (the module's tables don't exist before it is installed, or the database is
/// down) counts as spent, so live stays off instead of running without a budget. The failure is cached for
/// <see cref="ReadEvery"/> like a good read, so a down store isn't hammered, and the warning is logged at most once a minute.
/// <para>
/// Spent is synchronous (ITokenMeter): a read waits on the store's task. Neither the in-memory store nor an EF store
/// needs a synchronisation context, so that can't deadlock.
/// </para>
/// </remarks>
public sealed class TokenMeter(IRunStore runs, LiveOptions opt, TimeProvider tp, ILogger<TokenMeter>? log = null) : ITokenMeter
{
    public static readonly TimeSpan ReadEvery = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan WarnEvery = TimeSpan.FromMinutes(1);

    readonly ILogger _log = (ILogger?)log ?? NullLogger.Instance;
    DateTimeOffset? _warnedAt;
    bool _failed, _baseUnknown;

    readonly object _gate = new();
    DateOnly? _day;
    DateTimeOffset _readAt;
    long _base, _stored, _addedToday;

    /// <summary>
    /// Whether that day's budget is spent. A late call for an earlier day (a request that computed its day just before
    /// midnight) gets the current day's answer and changes nothing, so it can't roll the meter back a day.
    /// </summary>
    public bool Spent(DateOnly day)
    {
        lock (_gate)
        {
            if (_day is { } d && day < d) return SpentLocked();
            var now = tp.GetUtcNow();
            if (_day != day) StartDay(day, now);
            else if (now - _readAt >= ReadEvery)
            {
                TryRead(day, now);
                if (_baseUnknown && !_failed) { _base = _stored; _baseUnknown = false; }
            }
            return SpentLocked();
        }
    }

    bool SpentLocked() => _failed || Math.Max(_stored, _base + _addedToday) >= opt.DailyTokenBudget;

    /// <summary>Tokens a model call just used, on the day it was made. An earlier day's tokens are ignored.</summary>
    public void Add(DateOnly day, int tokens)
    {
        lock (_gate)
        {
            if (_day is { } d && day < d) return;
            if (_day != day) StartDay(day, tp.GetUtcNow());
            _addedToday += tokens;
        }
    }

    void StartDay(DateOnly day, DateTimeOffset now)
    {
        _addedToday = 0;
        _day = day;
        _stored = _base = 0;
        TryRead(day, now);
        _base = _stored;
        _baseUnknown = _failed;
    }

    /// <summary>Reads the store into _stored. On failure sets _failed (counts as spent) and warns, throttled.</summary>
    void TryRead(DateOnly day, DateTimeOffset now)
    {
        _readAt = now;
        try
        {
            _stored = runs.TokensTodayAsync(day).GetAwaiter().GetResult();
            _failed = false;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            _failed = true;
            if (_warnedAt is not { } w || now - w >= WarnEvery)
            {
                _warnedAt = now;
                _log.LogWarning("The token meter can't read today's usage, so live counts as off until it can ({Reason}). " +
                    "Expected before the NeuCharBoxLive module is installed in the NCF admin; after install it means the database is down or unreachable.", e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
