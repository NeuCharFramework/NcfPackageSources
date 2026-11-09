/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CopyGuard.cs
    文件功能描述：NeuCharBox Live 引擎（Ai）：CopyGuard


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.RegularExpressions;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

/// <summary>
/// Banned terms in model text and server-made labels. The list is verbatim from the demo's test/run.mjs GUARD,
/// case-insensitive. Change it there first.
/// </summary>
public static class CopyGuard
{
    const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    static readonly Regex[] Patterns = new[]
    {
        @"\bzigbee\b", @"\bmatter\b", @"\bthread\b", @"\bz-?wave\b", @"100\s?%", @"\bguarantee",
        @"home assistant(?![^.]{0,80}\b(?:bridge|in progress)\b)",
    }.Select(p => new Regex(p, Opts)).ToArray();

    static readonly Regex Spaces = new(" {2,}", RegexOptions.CultureInvariant);
    static readonly Regex SpaceBeforePunct = new(@" +(?=[.,;:!?])", RegexOptions.CultureInvariant);
    static readonly Regex SentenceBreak = new(@"(?<=[.!?])\s+(?=\p{Lu})", RegexOptions.CultureInvariant);

    /// <summary>The matched substrings, in pattern order.</summary>
    public static IReadOnlyList<string> Find(string text) =>
        Patterns.SelectMany(p => p.Matches(text).Select(m => m.Value)).ToList();

    /// <summary>Removes every match, collapses repeated spaces and trims spaces before punctuation.</summary>
    public static string Strip(string text)
    {
        foreach (var p in Patterns) text = p.Replace(text, "");
        text = Spaces.Replace(text, " ");
        text = SpaceBeforePunct.Replace(text, "");
        return text.Trim();
    }

    /// <summary>Where TwoSentences would cut the text (the second sentence break), or -1 when it wouldn't cut it.</summary>
    public static int TwoSentencesEnd(string text)
    {
        var breaks = SentenceBreak.Matches(text);
        return breaks.Count >= 2 ? breaks[1].Index : -1;
    }

    /// <summary>Keeps the first two sentences, split on ". ", "! " or "? " followed by an uppercase letter.</summary>
    public static string TwoSentences(string text)
    {
        var parts = SentenceBreak.Split(text.Trim());
        return parts.Length <= 2 ? text.Trim() : string.Join(" ", parts.Take(2));
    }
}
