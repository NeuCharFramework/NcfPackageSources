/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminViews.cs
    文件功能描述：后台页面共用的视图辅助：批准申请后给申请人的话术等


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 后台时间改用使用日所在时区（AdminTime.InZone16 取代 Eastern16）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>The ready-to-copy message the Applications page shows for an approved application (Leo sends it himself).</summary>
public static class InviteMessage
{
    public const string DemoUrl = "https://leozheng265.github.io/neucharbox-demo/";

    static readonly Regex SafeName = new(@"^[\p{L} '\-]{1,60}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>The applicant's name if it is only letters, spaces, apostrophes and hyphens (at most 60); otherwise "there". The name is typed by a stranger and Leo pastes this message, so no link, email or line break gets through.</summary>
    public static string Greeting(string? name)
    {
        var n = name?.Trim();
        return n is not null && SafeName.IsMatch(n) ? n : "there";
    }

    /// <summary>The daily limit is the code's own, so a code whose limit was changed says so.</summary>
    public static string For(string? name, InviteCode code) =>
        $"Hi {Greeting(name)}, here's your NeuCharBox live AI invite code: {code.Code}. " +
        $"Open {DemoUrl}, try a guided request, then press \"Try your own request (live AI)\". " +
        $"It's good for {code.DailyLimit.ToString(CultureInfo.InvariantCulture)} requests a day.";
}

/// <summary>What the Runs page shows in a run's outcome column.</summary>
public static class RunStatus
{
    /// <summary>When this host process started (the system clock if the OS won't say).</summary>
    public static readonly DateTimeOffset ProcessStarted = FindProcessStart();

    static DateTimeOffset FindProcessStart()
    {
        try { return new DateTimeOffset(System.Diagnostics.Process.GetCurrentProcess().StartTime); }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            return DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// The outcome of an ended run. A run with no end is "running" if this host process started it, and "unfinished" if
    /// it started before the host did: runs live in the process, so the server stopped mid-run and nothing will end it.
    /// </summary>
    public static string Of(RunRow r, DateTimeOffset hostStarted) =>
        r.End is { } e ? e.Outcome : r.Start.Started >= hostStarted ? "running" : "unfinished";
}

/// <summary>Text for a stored value (a status, an outcome) that the resx may or may not name: the raw value when there is no key.</summary>
public static class AdminText
{
    public static string Of(Microsoft.Extensions.Localization.IStringLocalizer localizer, string prefix, string value)
    {
        var s = localizer[prefix + value];
        return s.ResourceNotFound ? value : s.Value;
    }
}

/// <summary>Times on the admin pages, in the zone the usage day resets in (<c>UsageDay.Zone</c>).</summary>
public static class AdminTime
{
    public static string InZone16(DateTimeOffset t, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(t, zone).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}

/// <summary>
/// The Runs CSV export (RFC 4180, CRLF line ends, times in UTC). A run with no end leaves the end columns empty. Visitor
/// text that a spreadsheet would read as a formula (it starts with = + - @, a tab or a CR) gets a leading apostrophe.
/// </summary>
public static class RunsCsv
{
    public static readonly string[] Header =
    {
        "id", "started_utc", "day", "code", "scene", "model", "what_if", "what_if_device", "outcome", "calls",
        "prompt_tokens", "completion_tokens", "total_tokens", "latency_ms", "finished_utc", "text", "plan_json", "summary",
    };

    public static string Write(IEnumerable<RunRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(",", Header)).Append("\r\n");
        foreach (var r in rows)
        {
            var s = r.Start;
            var e = r.End;
            string[] fields =
            {
                Num(r.Id), Time(s.Started), s.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Text(s.Code), Text(s.Scene),
                Text(s.Model), s.WhatIf ? "true" : "false", Text(s.WhatIfDevice), Text(e?.Outcome),
                e is null ? "" : Num(e.Calls), e is null ? "" : Num(e.PromptTokens), e is null ? "" : Num(e.CompletionTokens),
                e is null ? "" : Num((long)e.PromptTokens + e.CompletionTokens), e is null ? "" : Num(e.LatencyMs),
                e is null ? "" : Time(e.Finished), Text(s.Text), Text(e?.PlanJson), Text(e?.Summary),
            };
            sb.Append(string.Join(",", fields)).Append("\r\n");
        }
        return sb.ToString();
    }

    static string Num(long n) => n.ToString(CultureInfo.InvariantCulture);

    static string Time(DateTimeOffset t) => t.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    static string Text(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        if (v[0] is '=' or '+' or '-' or '@' or '\t' or '\r') v = "'" + v;
        return v.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
}
