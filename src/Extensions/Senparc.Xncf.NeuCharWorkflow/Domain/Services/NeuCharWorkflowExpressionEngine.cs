/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowExpressionEngine.cs
    文件功能描述：Workflow 文本模板的受限表达式解释器
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
        "trim", "lower", "upper", "first", "last", "at", "join", "toNumber"
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
            return JsonValue.Create(Number(Arg(0)));
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
    private static int Length(JsonNode value) => value is JsonArray array ? array.Count : Text(value).Length;
    private static bool Contains(JsonNode value, JsonNode sought) => value is JsonArray array ? array.Any(x => Equal(x, sought)) : Text(value).Contains(Text(sought), StringComparison.OrdinalIgnoreCase);
    private static string Join(JsonNode value, string separator) => value is JsonArray array ? string.Join(separator, array.Select(Text)) : Text(value);
    private static string Text(JsonNode value)
    {
        if (value == null) return string.Empty;
        if (value is JsonValue json && json.TryGetValue<string>(out var text)) return text ?? string.Empty;
        return value.ToJsonString();
    }

    private sealed class ExpressionException(string message) : Exception(message) { }
}
