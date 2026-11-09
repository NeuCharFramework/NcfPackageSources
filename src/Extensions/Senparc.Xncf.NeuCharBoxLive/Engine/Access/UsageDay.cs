/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：UsageDay.cs
    文件功能描述：使用日：每日限额与令牌预算按所在时区的午夜重置


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>
/// The day the daily limits and the token budget count against: the date in <see cref="Zone"/>, so they reset at
/// midnight there. The host's LiveOptions.UsageTimeZone picks the zone (UTC by default); the container holds one.
/// </summary>
public sealed class UsageDay
{
    public UsageDay(TimeZoneInfo zone) => Zone = zone ?? throw new ArgumentNullException(nameof(zone));

    public TimeZoneInfo Zone { get; }

    /// <summary>Days flip at midnight UTC.</summary>
    public static UsageDay Utc { get; } = new(TimeZoneInfo.Utc);

    /// <summary>The date in <see cref="Zone"/> at that instant (its own offset doesn't matter). Today is <c>For(tp.GetUtcNow())</c>.</summary>
    public DateOnly For(DateTimeOffset utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, Zone).DateTime);

    /// <summary>
    /// The usage day for a time zone id (IANA, such as America/New_York, or Windows). Null, empty or blank is UTC. An id
    /// the system doesn't know is false, with <paramref name="day"/> set to <see cref="Utc"/>. Never throws.
    /// </summary>
    public static bool TryFromId(string? id, out UsageDay day)
    {
        day = Utc;
        if (string.IsNullOrWhiteSpace(id)) return true;
        try
        {
            day = new UsageDay(TimeZoneInfo.FindSystemTimeZoneById(id.Trim()));
            return true;
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException
                                      or System.Security.SecurityException)
        {
            return false;
        }
    }
}
