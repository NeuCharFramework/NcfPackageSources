/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Catalog.cs
    文件功能描述：NeuCharBox Live 引擎（Scenes）：Catalog


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

/// <summary>
/// One effect of an action. Op is "set" or "add". Arg takes an argument's value (times Scale for a number), Value a
/// constant. If applies the effect only when every listed argument equals that value. AfterArg/MsPerUnit/MinMs make the
/// delay max(MinMs, args[AfterArg] × MsPerUnit), a timed stop.
/// </summary>
public record CatalogEffect(string Op, string Field, string? Arg, JsonNode? Value, double? Scale, int Ms, int DelayMs, JsonObject? If, string? AfterArg, int MsPerUnit, int MinMs);

public record CatalogRead(string Field, int Round, string Unit);

/// <summary>A label case. When is null on the last, fallback case.</summary>
public record LabelCase(JsonObject? When, string Text);

/// <summary>Drift per simulated hour. Toward is a number or a field name of the same device.</summary>
public record CatalogDrift(string Field, double PerHour, JsonNode? Toward, JsonObject? While, double? Min, double? Max);

/// <summary>The shape of the editable-cards EditSpec: Kind is "number" or "bool".</summary>
public record CatalogEdit(string Field, string Kind, double? Min, double? Max);

public record CatalogAction(string Verb, ActionKind Kind, string Outcome, string Description, JsonObject Parameters, JsonObject? SafeWhen,
    IReadOnlyList<CatalogEffect> Effects, IReadOnlyList<CatalogRead> Reads, IReadOnlyList<LabelCase> Label);

public record CatalogType(string Type, string Name, string Ref, string Category, string Mount, bool New, JsonObject Initial, string FaultText,
    IReadOnlyList<CatalogAction> Actions, IReadOnlyList<CatalogEdit> Edit, IReadOnlyList<CatalogDrift> Drift);

public record SuggestionTemplate(IReadOnlyList<string> Needs, string Text);

public sealed record CatalogData(int MaxDevices, IReadOnlyList<string> Categories, IReadOnlyList<CatalogType> Types, IReadOnlyList<SuggestionTemplate> Suggestions)
{
    public CatalogType? Find(string type) => Types.FirstOrDefault(t => t.Type == type);
}

/// <summary>
/// The device catalog (Scenes/catalog.json, embedded): the source of truth for every device type in the custom scene.
/// </summary>
public static class Catalog
{
    static readonly Lazy<CatalogData> Shared = new(() =>
    {
        using var s = typeof(Catalog).Assembly.GetManifestResourceStream("Senparc.Xncf.NeuCharBoxLive.Engine.Scenes.catalog.json")
            ?? throw new KeyNotFoundException("the device catalog is not embedded");
        using var r = new StreamReader(s);
        return Parse(r.ReadToEnd());
    });

    /// <summary>The shipped catalog, parsed once and shared. Nothing may change its JSON nodes.</summary>
    public static CatalogData Load() => Shared.Value;

    /// <summary>Parses catalog JSON. Throws when a required key is missing. Every JSON node is materialized (see ManifestLoader).</summary>
    public static CatalogData Parse(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        var types = root["types"]!.AsArray().Select(t => ParseType(t!.AsObject())).ToList();
        var suggestions = root["suggestions"]!.AsArray().Select(s =>
            new SuggestionTemplate(s!["needs"]!.AsArray().Select(n => (string)n!).ToList(), (string)s["text"]!)).ToList();
        var categories = root["categories"]!.AsArray().Select(c => (string)c!).ToList();
        return new CatalogData((int)root["maxDevices"]!, categories, types, suggestions);
    }

    static CatalogType ParseType(JsonObject o) => new(
        (string)o["type"]!, (string)o["name"]!, (string)o["ref"]!, (string)o["category"]!, (string)o["mount"]!,
        o["new"] is { } nw && (bool)nw, Frozen(o["initial"]!.AsObject()), (string)o["faultText"]!,
        o["actions"]!.AsArray().Select(a => ParseAction(a!.AsObject())).ToList(),
        Items(o, "edit", e => new CatalogEdit((string)e["field"]!, (string)e["kind"]!, Num(e, "min"), Num(e, "max"))),
        Items(o, "drift", d => new CatalogDrift((string)d["field"]!, (double)d["perHour"]!,
            d["toward"] is { } tw ? tw.DeepClone() : null, d["while"] is JsonObject w ? Frozen(w) : null, Num(d, "min"), Num(d, "max"))));

    static CatalogAction ParseAction(JsonObject a) => new(
        (string)a["verb"]!, Enum.Parse<ActionKind>((string)a["kind"]!), a["outcome"] is { } oc ? (string)oc! : "confirmed",
        (string)a["description"]!, Frozen(a["parameters"]!.AsObject()), a["safeWhen"] is JsonObject sw ? Frozen(sw) : null,
        Items(a, "effects", e =>
        {
            var op = e["set"] is not null ? "set" : "add";
            return new CatalogEffect(op, (string)e[op]!, e["arg"] is { } arg ? (string)arg! : null, e["value"]?.DeepClone(),
                Num(e, "scale"), (int)(Num(e, "ms") ?? 0), (int)(Num(e, "delayMs") ?? 0), e["if"] is JsonObject i ? Frozen(i) : null,
                e["afterArg"] is { } aa ? (string)aa! : null, (int)(Num(e, "msPerUnit") ?? 0), (int)(Num(e, "minMs") ?? 0));
        }),
        Items(a, "reads", r => new CatalogRead((string)r["field"]!, (int)(Num(r, "round") ?? 0), r["unit"] is { } u ? (string)u! : "")),
        Items(a, "label", l => new LabelCase(l["when"] is JsonObject w ? Frozen(w) : null, (string)l["text"]!)));

    static List<T> Items<T>(JsonObject o, string key, Func<JsonObject, T> make) =>
        o[key] is JsonArray arr ? arr.Select(x => make(x!.AsObject())).ToList() : new List<T>();

    static double? Num(JsonObject o, string key) => o[key] is JsonValue v && v.TryGetValue<double>(out var d) ? d : null;

    /// <summary>A deep copy with every lazily built node built now, so sessions on several threads can read it at once.</summary>
    static JsonObject Frozen(JsonObject o)
    {
        var c = o.DeepClone().AsObject();
        Walk(c);
        return c;

        static void Walk(JsonNode? n)
        {
            switch (n)
            {
                case JsonObject obj:
                    foreach (var (_, v) in obj) Walk(v);
                    break;
                case JsonArray arr:
                    foreach (var v in arr) Walk(v);
                    break;
            }
        }
    }

    // ---- rules ----

    static readonly Regex TypeId = new(@"^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    static readonly Regex Verb = new(@"^[a-z]+$", RegexOptions.CultureInvariant);
    static readonly Regex Placeholder = new(@"\{([^{}]*)\}", RegexOptions.CultureInvariant);
    static readonly string[] Mounts = { "floor", "wall", "ceiling" };

    /// <summary>
    /// Every rule of the catalog format, as plain-language problems that each name their type id. Empty when clean.
    /// Never throws on a catalog that Parse accepted.
    /// </summary>
    public static IReadOnlyList<string> Problems(CatalogData c)
    {
        var p = new List<string>();
        if (c.MaxDevices != 20) p.Add($"catalog: maxDevices is {c.MaxDevices}, it must be 20");
        if (c.Categories.Count == 0) p.Add("catalog: no categories");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in c.Types)
        {
            if (!seen.Add(t.Type)) p.Add($"{t.Type}: duplicate type id");
            TypeProblems(c, t, p);
        }

        SuggestionProblems(c, p);
        return p;
    }

    static void TypeProblems(CatalogData c, CatalogType t, List<string> p)
    {
        void Bad(string what) => p.Add($"{t.Type}: {what}");

        if (!TypeId.IsMatch(t.Type)) Bad("the type id must be lowercase letters, digits and underscores, starting with a letter");
        if (t.Type.Length > 24) Bad("the type id is longer than 24 characters");
        if (!t.Ref.StartsWith("the ", StringComparison.Ordinal)) Bad($"ref '{t.Ref}' must start with \"the \"");
        if (!c.Categories.Contains(t.Category)) Bad($"category '{t.Category}' is not in the category list");
        if (!Mounts.Contains(t.Mount)) Bad($"mount '{t.Mount}' must be floor, wall or ceiling");
        if (t.Initial.ContainsKey("status")) Bad("initial must not hold status (the manifest adds it)");
        if (t.Actions.Count == 0) Bad("the type has no actions");

        Guard(Bad, "name", t.Name);
        Guard(Bad, "ref", t.Ref + " 20");
        Guard(Bad, "faultText", t.FaultText);

        // Edit
        foreach (var e in t.Edit)
        {
            if (e.Field == "status") { Bad("edit must not cover status"); continue; }
            if (!t.Initial.ContainsKey(e.Field)) { Bad($"edit field '{e.Field}' is not in initial"); continue; }
            var iv = t.Initial[e.Field];
            if (e.Kind == "number")
            {
                if (e.Min is null || e.Max is null) Bad($"edit '{e.Field}' is a number and needs both min and max");
                else if (e.Min >= e.Max) Bad($"edit '{e.Field}' needs min below max");
                else if (!IsNumber(iv, out var d)) Bad($"edit '{e.Field}' is a number but its initial value is not");
                else if (d < e.Min || d > e.Max) Bad($"edit '{e.Field}' initial value {d} is outside {e.Min} to {e.Max}");
            }
            else if (e.Kind == "bool")
            {
                if (e.Min is not null || e.Max is not null) Bad($"edit '{e.Field}' is a bool and takes no min or max");
                if (!(iv is JsonValue v && v.TryGetValue<bool>(out _))) Bad($"edit '{e.Field}' is a bool but its initial value is not");
            }
            else Bad($"edit '{e.Field}' has kind '{e.Kind}', it must be number or bool");
        }

        // Drift
        foreach (var d in t.Drift)
        {
            if (!t.Initial.ContainsKey(d.Field)) Bad($"drift field '{d.Field}' is not in initial");
            if (d.Toward is not null)
            {
                if (IsNumber(d.Toward, out _)) { }
                else if (d.Toward is JsonValue tv && tv.TryGetValue<string>(out var tf))
                {
                    if (!t.Initial.ContainsKey(tf)) Bad($"drift '{d.Field}' moves toward '{tf}', which is not in initial");
                }
                else Bad($"drift '{d.Field}' toward must be a number or a field name");
            }
            if (d.While is not null)
                foreach (var (k, _) in d.While)
                    if (!t.Initial.ContainsKey(k)) Bad($"drift '{d.Field}' while-field '{k}' is not in initial");
            if (d.Min is not null && d.Max is not null && d.Min > d.Max) Bad($"drift '{d.Field}' has min above max");
        }

        // Actions
        var verbs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var a in t.Actions) ActionProblems(t, a, verbs, Bad);
    }

    static void ActionProblems(CatalogType t, CatalogAction a, HashSet<string> verbs, Action<string> bad)
    {
        var at = $"action '{a.Verb}'";
        if (!Verb.IsMatch(a.Verb)) bad($"{at}: the verb must be lowercase letters only");
        if (!verbs.Add(a.Verb)) bad($"{at}: duplicate verb");
        if ($"{a.Verb}_{t.Type}_20".Length > 64) bad($"{at}: the action name would be longer than 64 characters");

        var props = (a.Parameters["properties"] as JsonObject)?.Select(kv => kv.Key).ToHashSet() ?? new HashSet<string>();
        if (a.Parameters["type"] is not JsonValue tv || !tv.TryGetValue<string>(out var ty) || ty != "object") bad($"{at}: parameters must be an object schema");
        if (a.Parameters["properties"] is not JsonObject) bad($"{at}: parameters need a properties object");
        if (!(a.Parameters["additionalProperties"] is JsonValue ap && ap.TryGetValue<bool>(out var apv) && !apv)) bad($"{at}: parameters must set additionalProperties to false");
        var required = (a.Parameters["required"] as JsonArray)?.Select(x => x is JsonValue v && v.TryGetValue<string>(out var s) ? s : "").ToList() ?? new List<string>();
        foreach (var r in required)
            if (!props.Contains(r)) bad($"{at}: required argument '{r}' is not in properties");
        if (a.SafeWhen is not null)
            foreach (var (k, _) in a.SafeWhen)
                if (!props.Contains(k)) bad($"{at}: safeWhen key '{k}' is not a parameter");

        switch (a.Kind)
        {
            case ActionKind.Read:
                if (a.Effects.Count > 0) bad($"{at}: a Read must not have effects");
                if (a.Reads.Count == 0) bad($"{at}: a Read needs at least one read");
                break;
            case ActionKind.Write:
            case ActionKind.Safe:
                if (a.Reads.Count > 0) bad($"{at}: only a Read may have reads");
                if (a.Effects.Count == 0) bad($"{at}: a {a.Kind} needs at least one effect");
                if (a.Kind == ActionKind.Safe && required.Count > 0) bad($"{at}: a Safe action must not take required arguments");
                if (a.Kind == ActionKind.Write && a.Outcome is not ("confirmed" or "commanded")) bad($"{at}: outcome '{a.Outcome}' must be confirmed or commanded");
                break;
        }

        foreach (var r in a.Reads)
            if (!t.Initial.ContainsKey(r.Field)) bad($"{at}: reads field '{r.Field}', which is not in initial");

        foreach (var e in a.Effects)
        {
            if (e.Op is not ("set" or "add")) bad($"{at}: effect op '{e.Op}' must be set or add");
            if (!t.Initial.ContainsKey(e.Field)) bad($"{at}: effect changes field '{e.Field}', which is not in initial");
            if ((e.Arg is null) == (e.Value is null)) bad($"{at}: effect on '{e.Field}' needs exactly one of arg and value");
            if (e.Arg is not null && !props.Contains(e.Arg)) bad($"{at}: effect on '{e.Field}' takes argument '{e.Arg}', which is not a parameter");
            if (e.Scale is not null && e.Arg is null) bad($"{at}: effect on '{e.Field}' has a scale but no arg");
            if (e.AfterArg is not null && !props.Contains(e.AfterArg)) bad($"{at}: effect on '{e.Field}' waits on '{e.AfterArg}', which is not a parameter");
            if (e.If is not null)
                foreach (var (k, _) in e.If)
                    if (!props.Contains(k)) bad($"{at}: effect on '{e.Field}' has an if-condition on '{k}', which is not a parameter");
            if (e.Ms < 0 || e.DelayMs < 0 || e.MsPerUnit < 0 || e.MinMs < 0) bad($"{at}: effect on '{e.Field}' has a negative time");
        }

        // Description
        var d = Placeholder.Replace(a.Description, m =>
        {
            if (m.Groups[1].Value is "ref") return t.Ref;
            if (m.Groups[1].Value is "name") return t.Name;
            bad($"{at}: description placeholder {m.Value} must be {{ref}} or {{name}}");
            return "";
        });
        Guard(bad, $"{at} description", d);

        // Label
        if (a.Label.Count == 0) { bad($"{at}: no label"); return; }
        for (var i = 0; i < a.Label.Count; i++)
        {
            var l = a.Label[i];
            var last = i == a.Label.Count - 1;
            if (last && l.When is not null) bad($"{at}: the last label case must have no 'when' (it is the fallback)");
            if (!last && l.When is null) bad($"{at}: only the last label case may have no 'when'");
            if (l.When is not null)
                foreach (var (k, _) in l.When)
                    if (!props.Contains(k)) bad($"{at}: label 'when' key '{k}' is not a parameter");
            var text = Placeholder.Replace(l.Text, m =>
            {
                var n = m.Groups[1].Value;
                if (n == "name") return t.Name;
                if (props.Contains(n)) return "0";
                bad($"{at}: label placeholder {m.Value} is not a parameter");
                return "";
            });
            Guard(bad, $"{at} label", text);
        }
    }

    static void SuggestionProblems(CatalogData c, List<string> p)
    {
        if (c.Suggestions.Count == 0) { p.Add("catalog: no suggestions"); return; }
        if (c.Suggestions[^1].Needs.Count != 0) p.Add("catalog: the last suggestion must have no needs (it is the catch-all)");
        for (var i = 0; i < c.Suggestions.Count; i++)
        {
            var s = c.Suggestions[i];
            var label = $"suggestion '{s.Text}'";
            if (i > 0 && s.Needs.Count > c.Suggestions[i - 1].Needs.Count) p.Add($"catalog: {label} has more needs than the one before it; list two needs first, then one");
            foreach (var n in s.Needs)
                if (c.Find(n) is null) p.Add($"catalog: {label} needs unknown type '{n}'");
            var text = Placeholder.Replace(s.Text, m =>
            {
                var n = m.Groups[1].Value;
                if (!s.Needs.Contains(n)) { p.Add($"catalog: {label} uses {m.Value}, which is not one of its needs"); return ""; }
                return c.Find(n)?.Ref ?? n;
            });
            var hits = CopyGuard.Find(text);
            if (hits.Count > 0) p.Add($"catalog: {label} trips the copy guard ({string.Join(", ", hits)})");
        }
    }

    static void Guard(Action<string> bad, string where, string text)
    {
        var hits = CopyGuard.Find(text);
        if (hits.Count > 0) bad($"{where} trips the copy guard ({string.Join(", ", hits)}): {text}");
    }

    static bool IsNumber(JsonNode? n, out double d)
    {
        d = 0;
        return n is JsonValue v && v.TryGetValue<double>(out d);
    }
}
