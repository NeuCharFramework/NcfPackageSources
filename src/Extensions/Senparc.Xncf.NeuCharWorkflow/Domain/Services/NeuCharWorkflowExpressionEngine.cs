/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowExpressionEngine.cs
    文件功能描述：Workflow 文本模板的受限表达式解释器


    创建标识：Senparc - 20260811

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// A deliberately small expression language for <c>{{= ... }}</c> template fragments.
/// It has no reflection, assignment, loops, host objects or JavaScript evaluation.
/// </summary>
public static class NeuCharWorkflowExpressionEngine
{
    private const int MaxLength = 512;
    private const int MaxDepth = 32;
    private static readonly HashSet<string> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "coalesce", "contains", "startsWith", "endsWith", "length", "substring",
        "trim", "lower", "upper", "first", "last", "at", "join", "toNumber",
        "toInt", "toLong", "toDecimal", "toBool", "toString",
        "now", "formatDate", "split", "replace", "sort", "orderBy", "reverse",
        "take", "skip", "sum", "min", "max", "unique", "count", "toArray",
        "isEmpty", "isNull", "concat", "flatten", "keys", "values", "has"
    };

    public static bool TryValidate(string expression, IEnumerable<string> allowedVariables, out string error)
    {
        return TryEvaluate(expression, allowedVariables?.ToDictionary(x => x, _ => (JsonNode)null,
            StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, JsonNode>(StringComparer.OrdinalIgnoreCase),
            validateOnly: true, out _, out error);
    }

    public static bool TryEvaluate(
        string expression,
        IReadOnlyDictionary<string, JsonNode> variables,
        out JsonNode result,
        out string error)
    {
        return TryEvaluate(expression, variables, validateOnly: false, out result, out error);
    }

    private static bool TryEvaluate(
        string expression,
        IReadOnlyDictionary<string, JsonNode> variables,
        bool validateOnly,
        out JsonNode result,
        out string error)
    {
        result = null;
        error = null;
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > MaxLength)
        {
            error = "表达式不能为空，且最长 512 个字符。";
            return false;
        }

        try
        {
            var parser = new Parser(expression, variables, validateOnly);
            result = parser.Parse();
            return true;
        }
        catch (ExpressionException ex)
        {
            error = ex.Message;
            return false;
        }
        catch
        {
            error = "表达式无效。";
            return false;
        }
    }

    private sealed class Parser
    {
        private readonly string _text;
        private readonly IReadOnlyDictionary<string, JsonNode> _variables;
        private readonly bool _validateOnly;
        private int _position;
        private int _depth;

        public Parser(string text, IReadOnlyDictionary<string, JsonNode> variables, bool validateOnly)
        {
            _text = text;
            _variables = variables ?? throw new ExpressionException("表达式变量上下文无效。");
            _validateOnly = validateOnly;
        }

        public JsonNode Parse()
        {
            var value = Conditional();
            SkipWhitespace();
            if (_position != _text.Length)
            {
                throw Error("存在无法识别的内容");
            }
            return value;
        }

        private JsonNode Conditional()
        {
            var value = Or();
            if (TryConsume("?"))
            {
                var whenTrue = Conditional();
                Expect(":");
                var whenFalse = Conditional();
                return Bool(value) ? whenTrue : whenFalse;
            }
            return value;
        }

        private JsonNode Or()
        {
            var value = And();
            while (TryConsume("||")) value = JsonValue.Create(Bool(value) || Bool(And()));
            return value;
        }

        private JsonNode And()
        {
            var value = Equality();
            while (TryConsume("&&")) value = JsonValue.Create(Bool(value) && Bool(Equality()));
            return value;
        }

        private JsonNode Equality()
        {
            var value = Compare();
            while (true)
            {
                if (TryConsume("==")) value = JsonValue.Create(Equal(value, Compare()));
                else if (TryConsume("!=")) value = JsonValue.Create(!Equal(value, Compare()));
                else return value;
            }
        }

        private JsonNode Compare()
        {
            var value = Add();
            while (true)
            {
                if (TryConsume(">=")) value = JsonValue.Create(Number(value) >= Number(Add()));
                else if (TryConsume("<=")) value = JsonValue.Create(Number(value) <= Number(Add()));
                else if (TryConsume(">")) value = JsonValue.Create(Number(value) > Number(Add()));
                else if (TryConsume("<")) value = JsonValue.Create(Number(value) < Number(Add()));
                else return value;
            }
        }

        private JsonNode Add()
        {
            var value = Unary();
            while (true)
            {
                if (TryConsume("+"))
                {
                    var right = Unary();
                    value = IsNumber(value) && IsNumber(right)
                        ? JsonValue.Create(Number(value) + Number(right))
                        : JsonValue.Create(Text(value) + Text(right));
                }
                else if (TryConsume("-")) value = JsonValue.Create(Number(value) - Number(Unary()));
                else return value;
            }
        }

        private JsonNode Unary()
        {
            if (TryConsume("!")) return JsonValue.Create(!Bool(Unary()));
            if (TryConsume("-")) return JsonValue.Create(-Number(Unary()));
            return Primary();
        }

        private JsonNode Primary()
        {
            if (++_depth > MaxDepth) throw Error("嵌套层级超过 32 层");
            try
            {
                SkipWhitespace();
                JsonNode value;
                if (TryConsume("("))
                {
                    value = Conditional();
                    Expect(")");
                }
                else if (Peek() is '\'' or '"') value = JsonValue.Create(ReadString());
                else if (char.IsDigit(Peek()) || Peek() == '.') value = JsonValue.Create(ReadNumber());
                else
                {
                    var name = ReadIdentifier();
                    if (name.Equals("true", StringComparison.OrdinalIgnoreCase)) value = JsonValue.Create(true);
                    else if (name.Equals("false", StringComparison.OrdinalIgnoreCase)) value = JsonValue.Create(false);
                    else if (name.Equals("null", StringComparison.OrdinalIgnoreCase)) value = null;
                    else if (TryConsume("(")) value = Invoke(name);
                    else value = Variable(name);
                }

                while (true)
                {
                    if (TryConsume(".")) value = Property(value, ReadIdentifier());
                    else if (TryConsume("["))
                    {
                        var index = (int)Number(Conditional());
                        Expect("]");
                        value = Index(value, index);
                    }
                    else return value;
                }
            }
            finally { _depth--; }
        }

        private JsonNode Invoke(string name)
        {
            if (!Functions.Contains(name)) throw Error($"不支持函数“{name}”");
            var args = new List<JsonNode>();
            if (!TryConsume(")"))
            {
                do { args.Add(Conditional()); } while (TryConsume(","));
                Expect(")");
            }
            JsonNode Arg(int index) => index < args.Count ? args[index] : null;
            if (name.Equals("if", StringComparison.OrdinalIgnoreCase)) return Bool(Arg(0)) ? Arg(1) : Arg(2);
            if (name.Equals("coalesce", StringComparison.OrdinalIgnoreCase)) return args.FirstOrDefault(x => !Empty(x));
            if (name.Equals("contains", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Contains(Arg(0), Arg(1)));
            if (name.Equals("startsWith", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Text(Arg(0)).StartsWith(Text(Arg(1)), StringComparison.OrdinalIgnoreCase));
            if (name.Equals("endsWith", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Text(Arg(0)).EndsWith(Text(Arg(1)), StringComparison.OrdinalIgnoreCase));
            if (name.Equals("length", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Length(Arg(0)));
            if (name.Equals("count", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Length(Arg(0)));
            if (name.Equals("isEmpty", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Empty(Arg(0)));
            if (name.Equals("isNull", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Arg(0) == null);
            if (name.Equals("substring", StringComparison.OrdinalIgnoreCase))
            {
                var text = Text(Arg(0)); var start = Math.Clamp((int)Number(Arg(1)), 0, text.Length);
                var length = args.Count > 2 ? Math.Clamp((int)Number(Arg(2)), 0, text.Length - start) : text.Length - start;
                return JsonValue.Create(text.Substring(start, length));
            }
            if (name.Equals("trim", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Text(Arg(0)).Trim());
            if (name.Equals("lower", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Text(Arg(0)).ToLowerInvariant());
            if (name.Equals("upper", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Text(Arg(0)).ToUpperInvariant());
            if (name.Equals("first", StringComparison.OrdinalIgnoreCase)) return Index(Arg(0), 0);
            if (name.Equals("last", StringComparison.OrdinalIgnoreCase)) return Index(Arg(0), Length(Arg(0)) - 1);
            if (name.Equals("at", StringComparison.OrdinalIgnoreCase)) return Index(Arg(0), (int)Number(Arg(1)));
            if (name.Equals("join", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Join(Arg(0), Text(Arg(1))));
            if (name.Equals("concat", StringComparison.OrdinalIgnoreCase)) return Concat(args);
            if (name.Equals("toNumber", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("toDecimal", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Decimal(Arg(0)));
            if (name.Equals("toInt", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Int32(Arg(0)));
            if (name.Equals("toLong", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Int64(Arg(0)));
            if (name.Equals("toBool", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Boolean(Arg(0)));
            if (name.Equals("toString", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(Text(Arg(0)));
            if (name.Equals("now", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            if (name.Equals("formatDate", StringComparison.OrdinalIgnoreCase))
            {
                if (!DateTimeOffset.TryParse(Text(Arg(0)), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
                    throw Error("formatDate 的第一个参数必须是日期或 now() 的结果");
                var format = args.Count > 1 ? Text(Arg(1)) : "yyyy-MM-dd HH:mm:ss";
                if (format.Length > 80) throw Error("日期格式不能超过 80 个字符");
                return JsonValue.Create(date.ToString(format, CultureInfo.InvariantCulture));
            }
            if (name.Equals("split", StringComparison.OrdinalIgnoreCase))
                return new JsonArray(Text(Arg(0)).Split(Text(Arg(1)), StringSplitOptions.None).Select(item => JsonValue.Create(item)).ToArray());
            if (name.Equals("replace", StringComparison.OrdinalIgnoreCase))
                return JsonValue.Create(Text(Arg(0)).Replace(Text(Arg(1)), Text(Arg(2)), StringComparison.Ordinal));
            if (name.Equals("sort", StringComparison.OrdinalIgnoreCase) || name.Equals("orderBy", StringComparison.OrdinalIgnoreCase))
                return Sort(Arg(0), args.Count > 1 ? Text(Arg(1)) : string.Empty, args.Count > 2 ? Text(Arg(2)) : "asc");
            if (name.Equals("reverse", StringComparison.OrdinalIgnoreCase)) return Reverse(Arg(0));
            if (name.Equals("take", StringComparison.OrdinalIgnoreCase)) return Slice(Arg(0), 0, (int)Number(Arg(1)));
            if (name.Equals("skip", StringComparison.OrdinalIgnoreCase)) return Slice(Arg(0), (int)Number(Arg(1)), int.MaxValue);
            if (name.Equals("toArray", StringComparison.OrdinalIgnoreCase)) return ToArrayNode(Arg(0));
            if (name.Equals("flatten", StringComparison.OrdinalIgnoreCase)) return Flatten(Arg(0));
            if (name.Equals("sum", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(ToArray(Arg(0)).Sum(Number));
            if (name.Equals("min", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(ToArray(Arg(0)).Select(Number).DefaultIfEmpty(0).Min());
            if (name.Equals("max", StringComparison.OrdinalIgnoreCase)) return JsonValue.Create(ToArray(Arg(0)).Select(Number).DefaultIfEmpty(0).Max());
            if (name.Equals("unique", StringComparison.OrdinalIgnoreCase)) return Unique(Arg(0));
            if (name.Equals("keys", StringComparison.OrdinalIgnoreCase)) return Keys(Arg(0));
            if (name.Equals("values", StringComparison.OrdinalIgnoreCase)) return Values(Arg(0));
            if (name.Equals("has", StringComparison.OrdinalIgnoreCase)) return Has(Arg(0), Arg(1));
            throw Error($"不支持函数“{name}”");
        }

        private JsonNode Variable(string name)
        {
            if (!_variables.TryGetValue(name, out var value)) throw Error($"变量“{name}”未绑定");
            return _validateOnly ? JsonValue.Create(0) : value?.DeepClone();
        }

        private JsonNode Property(JsonNode value, string name)
        {
            if (value is JsonObject obj && obj.TryGetPropertyValue(name, out var property)) return property?.DeepClone();
            return _validateOnly ? JsonValue.Create(0) : null;
        }

        private JsonNode Index(JsonNode value, int index)
        {
            if (value is JsonArray array && index >= 0 && index < array.Count) return array[index]?.DeepClone();
            return _validateOnly ? JsonValue.Create(0) : null;
        }

        private char Peek()
        {
            SkipWhitespace();
            return _position < _text.Length ? _text[_position] : '\0';
        }

        private bool TryConsume(string token)
        {
            SkipWhitespace();
            if (!_text.AsSpan(_position).StartsWith(token, StringComparison.Ordinal)) return false;
            _position += token.Length;
            return true;
        }

        private void Expect(string token)
        {
            if (!TryConsume(token)) throw Error($"缺少“{token}”");
        }

        private string ReadIdentifier()
        {
            SkipWhitespace();
            var start = _position;
            if (_position >= _text.Length || !(_text[_position] == '_' || char.IsLetter(_text[_position]))) throw Error("需要标识符");
            _position++;
            while (_position < _text.Length && (_text[_position] == '_' || char.IsLetterOrDigit(_text[_position]))) _position++;
            return _text[start.._position];
        }

        private string ReadString()
        {
            var quote = Peek(); _position++;
            var builder = new StringBuilder();
            while (_position < _text.Length)
            {
                var character = _text[_position++];
                if (character == quote) return builder.ToString();
                if (character == '\\' && _position < _text.Length)
                {
                    var escaped = _text[_position++];
                    builder.Append(escaped switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => escaped });
                }
                else builder.Append(character);
            }
            throw Error("字符串缺少结束引号");
        }

        private decimal ReadNumber()
        {
            SkipWhitespace();
            var start = _position;
            while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '.')) _position++;
            if (!decimal.TryParse(_text[start.._position], NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) throw Error("数字无效");
            return value;
        }

        private void SkipWhitespace() { while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++; }
        private ExpressionException Error(string message) => new($"第 {_position + 1} 个字符：{message}。");
    }

    private static bool Empty(JsonNode value) => value == null || (value is JsonValue && string.IsNullOrEmpty(Text(value))) || value is JsonArray array && array.Count == 0;
    private static bool Bool(JsonNode value) => value is JsonValue json && json.TryGetValue<bool>(out var boolean) ? boolean : !Empty(value) && !string.Equals(Text(value), "false", StringComparison.OrdinalIgnoreCase) && Text(value) != "0";
    private static bool Equal(JsonNode left, JsonNode right) => string.Equals(Text(left), Text(right), StringComparison.Ordinal);
    private static bool IsNumber(JsonNode value) => decimal.TryParse(Text(value), NumberStyles.Number, CultureInfo.InvariantCulture, out _);
    private static decimal Number(JsonNode value) => decimal.TryParse(Text(value), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : throw new ExpressionException("需要数字参数。");
    private static int Int32(JsonNode value) => int.TryParse(Text(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
        ? number
        : throw ConversionError(value, "Int32");
    private static long Int64(JsonNode value) => long.TryParse(Text(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
        ? number
        : throw ConversionError(value, "Int64");
    private static decimal Decimal(JsonNode value) => decimal.TryParse(Text(value).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
        ? number
        : throw ConversionError(value, "Decimal");
    private static bool Boolean(JsonNode value)
    {
        if (value is JsonValue json && json.TryGetValue<bool>(out var boolean)) return boolean;
        var text = Text(value).Trim();
        if (bool.TryParse(text, out boolean)) return boolean;
        if (text == "1") return true;
        if (text == "0") return false;
        throw ConversionError(value, "Boolean");
    }
    private static ExpressionException ConversionError(JsonNode value, string typeName)
    {
        var text = Text(value);
        var preview = text.Length <= 120 ? text : $"{text[..120]}…";
        return new ExpressionException($"无法将“{preview}”转换为 {typeName}。");
    }
    private static int Length(JsonNode value) => value is JsonArray array ? array.Count : Text(value).Length;
    private static bool Contains(JsonNode value, JsonNode sought) => value is JsonArray array ? array.Any(x => Equal(x, sought)) : Text(value).Contains(Text(sought), StringComparison.OrdinalIgnoreCase);
    private static string Join(JsonNode value, string separator) => value is JsonArray array ? string.Join(separator, array.Select(Text)) : Text(value);
    private static IEnumerable<JsonNode> ToArray(JsonNode value) => value is JsonArray array
        ? array.Where(item => item != null).Select(item => item!)
        : value == null ? Enumerable.Empty<JsonNode>() : new[] { value };
    private static JsonNode ToArrayNode(JsonNode value) => new JsonArray(ToArray(value).Select(item => item.DeepClone()).ToArray());
    private static JsonNode Concat(IReadOnlyList<JsonNode> values)
    {
        if (values.Any(value => value is JsonArray))
            return new JsonArray(values.SelectMany(ToArray).Select(item => item.DeepClone()).ToArray());
        return JsonValue.Create(string.Concat(values.Select(Text)));
    }
    private static JsonNode Flatten(JsonNode value) => new JsonArray(ToArray(value)
        .SelectMany(item => item is JsonArray nested ? nested.Where(child => child != null) : new[] { item })
        .Select(item => item.DeepClone()).ToArray());
    private static JsonNode Keys(JsonNode value) => value is JsonObject obj
        ? new JsonArray(obj.Select(item => JsonValue.Create(item.Key)).ToArray())
        : new JsonArray();
    private static JsonNode Values(JsonNode value) => value is JsonObject obj
        ? new JsonArray(obj.Select(item => item.Value).Where(item => item != null).Select(item => item!.DeepClone()).ToArray())
        : new JsonArray();
    private static JsonNode Has(JsonNode value, JsonNode key) => JsonValue.Create(value is JsonObject obj &&
        obj.Any(item => string.Equals(item.Key, Text(key), StringComparison.OrdinalIgnoreCase)));
    private static JsonNode Reverse(JsonNode value) => new JsonArray(ToArray(value).Reverse().Select(item => item.DeepClone()).ToArray());
    private static JsonNode Slice(JsonNode value, int skip, int take) => new JsonArray(ToArray(value)
        .Skip(Math.Max(0, skip)).Take(Math.Max(0, take)).Select(item => item.DeepClone()).ToArray());
    private static JsonNode Unique(JsonNode value) => new JsonArray(ToArray(value)
        .GroupBy(Text, StringComparer.Ordinal).Select(group => group.First().DeepClone()).ToArray());
    private static JsonNode Sort(JsonNode value, string path, string direction)
    {
        if (value is not JsonArray array) return value?.DeepClone() ?? JsonValue.Create(string.Empty);
        if (!string.IsNullOrWhiteSpace(path) && !path.Split('.').All(segment =>
                segment.Length > 0 && segment.All(character => char.IsLetterOrDigit(character) || character == '_')))
            throw new ExpressionException("sort/orderBy 的字段路径只能包含字母、数字、下划线和点。");
        if (!string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
            throw new ExpressionException("sort/orderBy 的排序方向只能是 asc 或 desc。");
        var items = array.Select((item, index) => new { Item = item, Index = index, Key = ReadPath(item, path) });
        var result = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase)
            ? items.OrderByDescending(item => SortKey(item.Key), StringComparer.Ordinal).ThenBy(item => item.Index)
            : items.OrderBy(item => SortKey(item.Key), StringComparer.Ordinal).ThenBy(item => item.Index);
        return new JsonArray(result.Select(item => item.Item?.DeepClone()).ToArray());
    }
    private static JsonNode ReadPath(JsonNode value, string path)
    {
        var current = value;
        foreach (var segment in (path ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(segment, out current)) return null;
        }
        return current;
    }
    private static string SortKey(JsonNode value) => decimal.TryParse(Text(value), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
        ? number.ToString("00000000000000000000000000000000.0000000000000000000000000000", CultureInfo.InvariantCulture)
        : Text(value);
    private static string Text(JsonNode value)
    {
        if (value == null) return string.Empty;
        if (value is JsonValue json && json.TryGetValue<string>(out var text)) return text ?? string.Empty;
        return value.ToJsonString();
    }

    private sealed class ExpressionException(string message) : Exception(message) { }
}
