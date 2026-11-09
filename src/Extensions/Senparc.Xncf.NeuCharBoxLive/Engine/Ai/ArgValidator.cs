/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ArgValidator.cs
    文件功能描述：按 JSON Schema 子集校验模型传来的工具参数


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

public record SplitResult(JsonObject? Args, string? Step, string? Error);

public record ValidationResult(bool Ok, JsonObject? Clean, string? Error);

/// <summary>
/// Checks the args a model sent against a JSON Schema subset: type (one or a list), enum, properties, required,
/// additionalProperties:false, items, minimum/maximum, minLength/maxLength, pattern and minItems/maxItems.
/// It never coerces ("20%" is not 20, "true" is not true) and never throws on bad input. Missing args (null) are {}.
/// Two departures from JSON Schema: a pattern must match the whole string, and a null on an optional property is absent.
/// </summary>
public static class ArgValidator
{
    const string NotAnObject = "args must be a JSON object";

    /// <summary>Moves "step" out of a device action's args. A JSON null step counts as no step.</summary>
    public static SplitResult SplitStep(JsonNode? args)
    {
        var o = ToObject(args);
        if (o is null) return new(null, null, NotAnObject);
        if (!o.TryGetPropertyValue("step", out var node)) return new(o, null, null);
        if (node is not null && Str(node) is null) return new(null, null, "'step' must be a string: the id of an approved plan step");
        o.Remove("step");
        return new(o, Str(node), null);
    }

    /// <summary>Clean is a copy of the args, parsed once if they came as a JSON string.</summary>
    public static ValidationResult Validate(JsonObject schema, JsonNode? args)
    {
        var o = ToObject(args);
        if (o is null) return new(false, null, NotAnObject);
        var error = Check(schema, o, "");
        return error is null ? new(true, o, null) : new(false, null, error);
    }

    /// <summary>JSON equality where numbers compare by value, so 0, 0.0 and 0e0 are the same.</summary>
    internal static bool Same(JsonNode? a, JsonNode? b) =>
        Num(a) is double x && Num(b) is double y ? x == y : JsonNode.DeepEquals(a, b);

    internal static string? Str(JsonNode? v) =>
        v is JsonValue j && j.GetValueKind() == JsonValueKind.String && j.TryGetValue<string>(out var s) ? s : null;

    internal static double? Num(JsonNode? v) =>
        v is JsonValue j && j.GetValueKind() == JsonValueKind.Number
            && double.TryParse(j.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d)
            ? d : null;

    /// <summary>A fresh copy of args as an object, or null when they aren't one.</summary>
    static JsonObject? ToObject(JsonNode? args)
    {
        try
        {
            if (args is null) return new JsonObject();
            if (Str(args) is string text) args = JsonNode.Parse(text);
            if (args is not JsonObject o) return null;
            var copy = (JsonObject)o.DeepClone();
            Touch(copy);
            return copy;
        }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException)
        {
            return null; // not JSON, or an object with a repeated key
        }
    }

    /// <summary>A parsed JsonObject builds its keys on first use, so a repeated key throws then. Make that happen here.</summary>
    static void Touch(JsonNode? n)
    {
        if (n is JsonObject o) foreach (var (_, v) in o) Touch(v);
        else if (n is JsonArray a) foreach (var v in a) Touch(v);
    }

    static string? Check(JsonObject s, JsonNode? v, string path)
    {
        var name = path.Length == 0 ? "args" : $"'{path}'";
        var types = s["type"] is JsonArray list ? list.Select(Str).OfType<string>().ToList()
            : Str(s["type"]) is string one ? new List<string> { one } : new List<string>();
        if (types.Count > 0 && !types.Any(t => Is(t, v))) return $"{name} must be {Or(types.Select(An))}";
        if (s["enum"] is JsonArray options && !options.Any(e => Same(e, v)))
            return $"{name} must be one of: {string.Join(", ", options.Select(Show))}";

        switch (v)
        {
            case JsonObject o: return CheckObject(s, o, path);
            case JsonArray a: return CheckArray(s, a, path);
        }
        if (Num(v) is double d)
        {
            if (Num(s["minimum"]) is double min && d < min) return $"{name} must be at least {Show(s["minimum"])}";
            if (Num(s["maximum"]) is double max && d > max) return $"{name} must be at most {Show(s["maximum"])}";
        }
        if (Str(v) is string text)
        {
            if (Num(s["minLength"]) is double minLen && text.Length < minLen)
                return minLen <= 1 ? $"{name} must not be empty" : $"{name} must be at least {Show(s["minLength"])} characters";
            if (Num(s["maxLength"]) is double maxLen && text.Length > maxLen) return $"{name} must be at most {Show(s["maxLength"])} characters";
            if (Str(s["pattern"]) is string pattern && !Matches(text, pattern))
                return $"{name} must be {Str(s["description"]) ?? $"like {pattern}"}";
        }
        return null;
    }

    /// <summary>A null on a known property that isn't required counts as left out, and is dropped from the copy.</summary>
    static string? CheckObject(JsonObject s, JsonObject o, string path)
    {
        var required = (s["required"] as JsonArray)?.Select(Str).OfType<string>().ToHashSet() ?? new HashSet<string>();
        foreach (var key in required)
            if (!o.ContainsKey(key)) return $"missing '{Sub(path, key)}'";
        var closed = s["additionalProperties"] is JsonValue ap && ap.GetValueKind() == JsonValueKind.False;
        var absent = new List<string>();
        foreach (var (key, value) in o)
        {
            if (s["properties"]?[key] is JsonObject p)
            {
                if (value is null && !required.Contains(key)) absent.Add(key);
                else if (Check(p, value, Sub(path, key)) is string error) return error;
            }
            else if (closed) return $"unexpected '{Sub(path, key)}'";
        }
        foreach (var key in absent) o.Remove(key);
        return null;
    }

    static string? CheckArray(JsonObject s, JsonArray a, string path)
    {
        if (Num(s["minItems"]) is double min && a.Count < min) return $"'{path}' needs at least {Show(s["minItems"])} item{(min == 1 ? "" : "s")}";
        if (Num(s["maxItems"]) is double max && a.Count > max) return $"'{path}' takes at most {Show(s["maxItems"])} items";
        if (s["items"] is JsonObject item)
            for (var i = 0; i < a.Count; i++)
                if (Check(item, a[i], $"{path}[{i}]") is string error) return error;
        return null;
    }

    static bool Is(string type, JsonNode? v) => type switch
    {
        "object" => v is JsonObject,
        "array" => v is JsonArray,
        "string" => Str(v) is not null,
        "boolean" => v is JsonValue b && b.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
        "number" => Num(v) is not null,
        "integer" => Num(v) is double d && d == Math.Floor(d),
        "null" => v is null,
        _ => false,
    };

    /// <summary>The match must span the whole string: .NET's $ also matches before a final "\n", so "18:00\n" would pass ^…$.</summary>
    static bool Matches(string text, string pattern)
    {
        try { return Regex.IsMatch(text, $@"\A(?:{pattern})\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)); }
        catch (RegexMatchTimeoutException) { return false; }
    }

    static string An(string type) => type switch
    {
        "integer" or "array" or "object" => $"an {type}",
        "null" => "null",
        _ => $"a {type}",
    };

    static string Or(IEnumerable<string> parts)
    {
        var p = parts.ToList();
        return p.Count == 1 ? p[0] : $"{string.Join(", ", p.Take(p.Count - 1))} or {p[^1]}";
    }

    static string Show(JsonNode? n) => Str(n) ?? n?.ToJsonString() ?? "null";

    static string Sub(string path, string key) => path.Length == 0 ? key : $"{path}.{key}";
}
