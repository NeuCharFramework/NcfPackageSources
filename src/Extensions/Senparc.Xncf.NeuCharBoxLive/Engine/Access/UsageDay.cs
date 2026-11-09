/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：UsageDay.cs
    文件功能描述：NeuCharBox Live 引擎（Access）：UsageDay


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>Daily limits reset at midnight America/New_York (global constraints).</summary>
public static class UsageDay
{
    static readonly TimeZoneInfo Eastern = FindEastern();

    /// <summary>The date in New York at that instant. Today is <c>UsageDay.For(tp.GetUtcNow())</c>.</summary>
    public static DateOnly For(DateTimeOffset utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, Eastern).DateTime);

    static TimeZoneInfo FindEastern()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
    }
}
