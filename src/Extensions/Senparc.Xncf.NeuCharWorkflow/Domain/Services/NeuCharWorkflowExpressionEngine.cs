/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowExpressionEngine.cs
    File description: Restricted expression interpreter for Workflow text templates

    Created by: Senparc - 20260811

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance Workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance Workflow function calls, task control, and replay management

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions TextJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private static readonly HashSet<string> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "coalesce", "contains", "startsWith", "endsWith", "length", "substring",
        "trim", "lower", "upper", "first", "last", "at", "join", "toNumber",
        "toInt", "toLong", "toDecimal", "toBool", "toString",
        "now", "formatDate", "split", "replace", "sort", "orderBy", "reverse",
        "take", "skip", "sum", "min", "max", "unique", "count", "toArray",
        "isEmpty", "isNull", "concat", "flatten", "keys", "values", "has"
    };

    /// <summary>
    /// Validates an expression against a set of allowed variable names without evaluating
    /// the variables' runtime values. The method parses the complete expression and returns
    /// parser or expression errors through <paramref name="error"/>.
    /// </summary>
    /// <param name="expression">The expression text to validate. It must be non-empty and no longer than 512 characters.</param>
    /// <param name="allowedVariables">The variable names that may be referenced by the expression.</param>
    /// <param name="error">Receives a user-readable validation error when validation fails; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the expression is syntactically valid and uses only supported functions and variables; otherwise, <see langword="false"/>.</returns>
    public static bool TryValidate(string expression, IEnumerable<string> allowedVariables, out string? error)
    {
        return TryEvaluate(expression, allowedVariables?.ToDictionary(x => x, _ => (JsonNode?)null,
            StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase),
            validateOnly: true, out _, out error);
    }

    /// <summary>
    /// Evaluates a restricted workflow expression against a JSON-compatible variable context.
    /// The interpreter supports only the operators and functions implemented by this class;
    /// it does not execute reflection, assignments, loops, host objects, or JavaScript.
    /// </summary>
    /// <param name="expression">The expression text to evaluate. It must be non-empty and no longer than 512 characters.</param>
    /// <param name="variables">The case-insensitive variable context available to the expression.</param>
    /// <param name="result">Receives the evaluated JSON value when evaluation succeeds.</param>
    /// <param name="error">Receives a user-readable evaluation error when evaluation fails; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the expression is evaluated successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryEvaluate(
        string expression,
        IReadOnlyDictionary<string, JsonNode?> variables,
        out JsonNode? result,
        out string? error)
    {
        return TryEvaluate(expression, variables, validateOnly: false, out result, out error);
    }

    /// <summary>
    /// Parses and optionally evaluates an expression using the selected validation mode.
    /// Expected expression failures are converted into the output error instead of escaping
    /// to callers, so the public validation and evaluation APIs can be used for user input.
    /// </summary>
    /// <param name="expression">The expression text to parse.</param>
    /// <param name="variables">The variable context used during evaluation or validation.</param>
    /// <param name="validateOnly">When <see langword="true"/>, resolves variables with placeholder values and suppresses runtime value access.</param>
    /// <param name="result">Receives the parsed or evaluated JSON value.</param>
    /// <param name="error">Receives the parser error, or a generic invalid-expression message for unexpected failures.</param>
    /// <returns><see langword="true"/> when parsing and evaluation complete successfully.</returns>
    private static bool TryEvaluate(
        string expression,
        IReadOnlyDictionary<string, JsonNode?> variables,
        bool validateOnly,
        out JsonNode? result,
        out string? error)
    {
        result = null;
        error = null;
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > MaxLength)
        {
            error = "The expression is required and must be at most 512 characters.";
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
            error = "The expression is invalid.";
            return false;
        }
    }

    private sealed class Parser
    {
        private readonly string _text;
        private readonly IReadOnlyDictionary<string, JsonNode?> _variables;
        private readonly bool _validateOnly;
        private int _position;
        private int _depth;

        /// <summary>
        /// Initializes a parser for one expression and one variable context.
        /// </summary>
        /// <param name="text">The expression text to parse.</param>
        /// <param name="variables">The variable context used to resolve identifiers.</param>
        /// <param name="validateOnly">Whether variable values should be replaced with validation placeholders.</param>
        /// <exception cref="ExpressionException">Thrown when the variable context is <see langword="null"/>.</exception>
        public Parser(string text, IReadOnlyDictionary<string, JsonNode?> variables, bool validateOnly)
        {
            _text = text;
            _variables = variables ?? throw new ExpressionException("The expression variable context is invalid.");
            _validateOnly = validateOnly;
        }

        /// <summary>
        /// Parses the complete expression and rejects any non-whitespace content left
        /// after the root expression has been consumed.
        /// </summary>
        /// <returns>The parsed or evaluated JSON value.</returns>
        /// <exception cref="ExpressionException">Thrown when trailing content or another expression error is encountered.</exception>
        public JsonNode? Parse()
        {
            var value = Conditional();
            SkipWhitespace();
            if (_position != _text.Length)
            {
                throw Error("Unrecognized content was found");
            }
            return value;
        }

        /// <summary>
        /// Parses a conditional expression in the form <c>condition ? whenTrue : whenFalse</c>.
        /// Conditional branches are parsed recursively so nested conditionals retain their
        /// right-associative behavior.
        /// </summary>
        /// <returns>The selected branch value, or the condition value when no conditional operator is present.</returns>
        private JsonNode? Conditional()
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

        /// <summary>
        /// Parses one or more boolean operands joined by the logical OR operator.
        /// </summary>
        /// <returns>A JSON boolean containing the combined truth value.</returns>
        private JsonNode? Or()
        {
            var value = And();
            while (TryConsume("||")) value = JsonValue.Create(Bool(value) || Bool(And()));
            return value;
        }

        /// <summary>
        /// Parses one or more boolean operands joined by the logical AND operator.
        /// </summary>
        /// <returns>A JSON boolean containing the combined truth value.</returns>
        private JsonNode? And()
        {
            var value = Equality();
            while (TryConsume("&&")) value = JsonValue.Create(Bool(value) && Bool(Equality()));
            return value;
        }

        /// <summary>
        /// Parses equality and inequality comparisons using the textual representation
        /// produced by the expression value conversion rules.
        /// </summary>
        /// <returns>A JSON boolean when comparisons are present, otherwise the nested value.</returns>
        private JsonNode? Equality()
        {
            var value = Compare();
            while (true)
            {
                if (TryConsume("==")) value = JsonValue.Create(Equal(value, Compare()));
                else if (TryConsume("!=")) value = JsonValue.Create(!Equal(value, Compare()));
                else return value;
            }
        }

        /// <summary>
        /// Parses numeric comparison operators and applies them from left to right.
        /// </summary>
        /// <returns>A JSON boolean when a comparison is present, otherwise the nested value.</returns>
        private JsonNode? Compare()
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

        /// <summary>
        /// Parses addition and subtraction. Addition preserves numeric arithmetic when
        /// both operands are numeric and otherwise concatenates their text representations.
        /// </summary>
        /// <returns>The arithmetic or concatenated JSON value.</returns>
        private JsonNode? Add()
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

        /// <summary>
        /// Parses the unary logical-not and numeric-negation operators before primary values.
        /// </summary>
        /// <returns>The transformed operand or the next primary value.</returns>
        private JsonNode? Unary()
        {
            if (TryConsume("!")) return JsonValue.Create(!Bool(Unary()));
            if (TryConsume("-")) return JsonValue.Create(-Number(Unary()));
            return Primary();
        }

        /// <summary>
        /// Parses literals, variables, function calls, parenthesized expressions, property
        /// access, and array indexing while enforcing the maximum nesting depth.
        /// </summary>
        /// <returns>The primary JSON value after member and index access are applied.</returns>
        /// <exception cref="ExpressionException">Thrown when the expression contains an invalid token, exceeds the nesting limit, or has an invalid access operation.</exception>
        private JsonNode? Primary()
        {
            if (++_depth > MaxDepth) throw Error("Nesting exceeds 32 levels");
            try
            {
                SkipWhitespace();
                JsonNode? value;
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

        /// <summary>
        /// Parses and executes one of the allow-listed expression functions, including
        /// collection, string, conversion, date, and numeric aggregation functions.
        /// </summary>
        /// <param name="name">The case-insensitive function name.</param>
        /// <returns>The function result as a JSON value.</returns>
        /// <exception cref="ExpressionException">Thrown when the function is not supported or one of its arguments cannot be converted as required.</exception>
        private JsonNode? Invoke(string name)
        {
            if (!Functions.Contains(name)) throw Error($"Function “{name}” is not supported");
            var args = new List<JsonNode?>();
            if (!TryConsume(")"))
            {
                do { args.Add(Conditional()); } while (TryConsume(","));
                Expect(")");
            }
            JsonNode? Arg(int index) => index < args.Count ? args[index] : null;
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
                    throw Error("The first formatDate argument must be a date or the result of now()");
                var format = args.Count > 1 ? Text(Arg(1)) : "yyyy-MM-dd HH:mm:ss";
                if (format.Length > 80) throw Error("The date format must be at most 80 characters");
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
            throw Error($"Function “{name}” is not supported");
        }

        /// <summary>
        /// Resolves a variable from the parser context and clones its value so expression
        /// evaluation cannot mutate the caller's JSON tree.
        /// </summary>
        /// <param name="name">The variable identifier to resolve.</param>
        /// <returns>The cloned variable value, or a validation placeholder in validation mode.</returns>
        /// <exception cref="ExpressionException">Thrown when the identifier is not present in the variable context.</exception>
        private JsonNode? Variable(string name)
        {
            if (!_variables.TryGetValue(name, out var value)) throw Error($"Variable “{name}” is not bound");
            return _validateOnly ? JsonValue.Create(0) : value?.DeepClone();
        }

        /// <summary>
        /// Reads a property from a JSON object. Missing properties produce a validation
        /// placeholder in validation mode and <see langword="null"/> during normal evaluation.
        /// </summary>
        /// <param name="value">The JSON value expected to contain the property.</param>
        /// <param name="name">The property name to read.</param>
        /// <returns>A cloned property value, a validation placeholder, or <see langword="null"/>.</returns>
        private JsonNode? Property(JsonNode? value, string name)
        {
            if (value is JsonObject obj && obj.TryGetPropertyValue(name, out var property)) return property?.DeepClone();
            return _validateOnly ? JsonValue.Create(0) : null;
        }

        /// <summary>
        /// Reads an element from a JSON array at the requested zero-based index.
        /// Out-of-range access produces a validation placeholder in validation mode and
        /// <see langword="null"/> during normal evaluation.
        /// </summary>
        /// <param name="value">The JSON value expected to be an array.</param>
        /// <param name="index">The zero-based array index.</param>
        /// <returns>A cloned array element, a validation placeholder, or <see langword="null"/>.</returns>
        private JsonNode? Index(JsonNode? value, int index)
        {
            if (value is JsonArray array && index >= 0 && index < array.Count) return array[index]?.DeepClone();
            return _validateOnly ? JsonValue.Create(0) : null;
        }

        /// <summary>
        /// Returns the next non-whitespace character without advancing the parser position.
        /// </summary>
        /// <returns>The next character, or the null character when the input is exhausted.</returns>
        private char Peek()
        {
            SkipWhitespace();
            return _position < _text.Length ? _text[_position] : '\0';
        }

        /// <summary>
        /// Consumes a token when it occurs at the current non-whitespace position.
        /// </summary>
        /// <param name="token">The exact token to match.</param>
        /// <returns><see langword="true"/> when the token was consumed; otherwise, <see langword="false"/>.</returns>
        private bool TryConsume(string token)
        {
            SkipWhitespace();
            if (!_text.AsSpan(_position).StartsWith(token, StringComparison.Ordinal)) return false;
            _position += token.Length;
            return true;
        }

        /// <summary>
        /// Requires a token at the current parser position and advances past it.
        /// </summary>
        /// <param name="token">The exact token required by the grammar.</param>
        /// <exception cref="ExpressionException">Thrown when the required token is not present.</exception>
        private void Expect(string token)
        {
            if (!TryConsume(token)) throw Error($"Missing “{token}”");
        }

        /// <summary>
        /// Reads an identifier beginning with a letter or underscore and continuing with
        /// letters, digits, or underscores.
        /// </summary>
        /// <returns>The identifier text.</returns>
        /// <exception cref="ExpressionException">Thrown when the current token is not a valid identifier.</exception>
        private string ReadIdentifier()
        {
            SkipWhitespace();
            var start = _position;
            if (_position >= _text.Length || !(_text[_position] == '_' || char.IsLetter(_text[_position]))) throw Error("An identifier is required");
            _position++;
            while (_position < _text.Length && (_text[_position] == '_' || char.IsLetterOrDigit(_text[_position]))) _position++;
            return _text[start.._position];
        }

        /// <summary>
        /// Reads a quoted string literal and handles the supported single-character escape sequences.
        /// </summary>
        /// <returns>The unquoted string value.</returns>
        /// <exception cref="ExpressionException">Thrown when the closing quote is missing.</exception>
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
            throw Error("The string is missing its closing quote");
        }

        /// <summary>
        /// Reads an invariant-culture decimal literal from the current parser position.
        /// </summary>
        /// <returns>The parsed decimal value.</returns>
        /// <exception cref="ExpressionException">Thrown when the numeric token is not a valid decimal.</exception>
        private decimal ReadNumber()
        {
            SkipWhitespace();
            var start = _position;
            while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '.')) _position++;
            if (!decimal.TryParse(_text[start.._position], NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) throw Error("The number is invalid");
            return value;
        }

        /// <summary>
        /// Advances the parser position over whitespace characters.
        /// </summary>
        private void SkipWhitespace() { while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++; }

        /// <summary>
        /// Creates an expression error annotated with the one-based parser character position.
        /// </summary>
        /// <param name="message">The grammar or conversion error detail.</param>
        /// <returns>An exception suitable for returning through the public error output.</returns>
        private ExpressionException Error(string message) => new($"Character {_position + 1}: {message}");
    }

    /// <summary>
    /// Determines whether a JSON value is null, an empty string, or an empty array.
    /// </summary>
    /// <param name="value">The JSON value to inspect.</param>
    /// <returns><see langword="true"/> when the value is empty according to expression semantics.</returns>
    private static bool Empty(JsonNode? value) => value == null || (value is JsonValue && string.IsNullOrEmpty(Text(value))) || value is JsonArray array && array.Count == 0;

    /// <summary>
    /// Converts a JSON value to the expression language's truthiness convention.
    /// </summary>
    /// <param name="value">The JSON value to convert.</param>
    /// <returns><see langword="false"/> for empty, <c>false</c>, and <c>0</c>; otherwise, <see langword="true"/>.</returns>
    private static bool Bool(JsonNode? value) => value is JsonValue json && json.TryGetValue<bool>(out var boolean) ? boolean : !Empty(value) && !string.Equals(Text(value), "false", StringComparison.OrdinalIgnoreCase) && Text(value) != "0";

    /// <summary>
    /// Compares two expression values using their invariant textual representations.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when both textual representations are equal.</returns>
    private static bool Equal(JsonNode? left, JsonNode? right) => string.Equals(Text(left), Text(right), StringComparison.Ordinal);

    /// <summary>
    /// Tests whether a value can be parsed as an invariant-culture decimal.
    /// </summary>
    /// <param name="value">The JSON value to inspect.</param>
    /// <returns><see langword="true"/> when the value has numeric text.</returns>
    private static bool IsNumber(JsonNode? value) => decimal.TryParse(Text(value), NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    /// <summary>
    /// Converts a JSON value to a decimal required by numeric expression operators.
    /// </summary>
    /// <param name="value">The JSON value to convert.</param>
    /// <returns>The invariant-culture decimal value.</returns>
    /// <exception cref="ExpressionException">Thrown when the value does not contain a valid number.</exception>
    private static decimal Number(JsonNode? value) => decimal.TryParse(Text(value), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : throw new ExpressionException("A numeric argument is required.");

    /// <summary>
    /// Converts a JSON value to a 32-bit integer using invariant-culture parsing.
    /// </summary>
    /// <param name="value">The JSON value to convert.</param>
    /// <returns>The converted integer.</returns>
    /// <exception cref="ExpressionException">Thrown when the value cannot be converted.</exception>
    private static int Int32(JsonNode? value) => int.TryParse(Text(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
        ? number
        : throw ConversionError(value, "Int32");

    /// <summary>
    /// Converts a JSON value to a 64-bit integer using invariant-culture parsing.
    /// </summary>
    /// <param name="value">The JSON value to convert.</param>
    /// <returns>The converted long integer.</returns>
    /// <exception cref="ExpressionException">Thrown when the value cannot be converted.</exception>
    private static long Int64(JsonNode? value) => long.TryParse(Text(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
        ? number
        : throw ConversionError(value, "Int64");

    /// <summary>
    /// Converts a JSON value to a decimal using invariant-culture parsing.
    /// </summary>
    /// <param name="value">The JSON value to convert.</param>
    /// <returns>The converted decimal.</returns>
    /// <exception cref="ExpressionException">Thrown when the value cannot be converted.</exception>
    private static decimal Decimal(JsonNode? value) => decimal.TryParse(Text(value).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
        ? number
        : throw ConversionError(value, "Decimal");

    /// <summary>
    /// Converts a JSON value to a boolean using Boolean text, <c>1</c>, or <c>0</c>.
    /// </summary>
    /// <param name="value">The JSON value to convert.</param>
    /// <returns>The converted Boolean value.</returns>
    /// <exception cref="ExpressionException">Thrown when the value is not a supported Boolean representation.</exception>
    private static bool Boolean(JsonNode? value)
    {
        if (value is JsonValue json && json.TryGetValue<bool>(out var boolean)) return boolean;
        var text = Text(value).Trim();
        if (bool.TryParse(text, out boolean)) return boolean;
        if (text == "1") return true;
        if (text == "0") return false;
        throw ConversionError(value, "Boolean");
    }
    /// <summary>
    /// Creates a bounded conversion error that includes a safe preview of the source value.
    /// </summary>
    /// <param name="value">The value that failed conversion.</param>
    /// <param name="typeName">The target type name used in the error message.</param>
    /// <returns>An expression exception describing the failed conversion.</returns>
    private static ExpressionException ConversionError(JsonNode? value, string typeName)
    {
        var text = Text(value);
        var preview = text.Length <= 120 ? text : $"{text[..120]}…";
        return new ExpressionException($"Cannot convert “{preview}” to {typeName}.");
    }
    /// <summary>
    /// Returns the element count of an array or the text length of a scalar value.
    /// </summary>
    /// <param name="value">The value to measure.</param>
    /// <returns>The array count or text length.</returns>
    private static int Length(JsonNode? value) => value is JsonArray array ? array.Count : Text(value).Length;

    /// <summary>
    /// Tests membership for arrays and performs case-insensitive substring matching for scalars.
    /// </summary>
    /// <param name="value">The value or array to search.</param>
    /// <param name="sought">The value to locate.</param>
    /// <returns><see langword="true"/> when the sought value is present.</returns>
    private static bool Contains(JsonNode? value, JsonNode? sought) => value is JsonArray array ? array.Any(x => Equal(x, sought)) : Text(value).Contains(Text(sought), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Joins array elements with the supplied separator, or returns scalar text unchanged.
    /// </summary>
    /// <param name="value">The value or array to join.</param>
    /// <param name="separator">The separator inserted between array elements.</param>
    /// <returns>The joined text.</returns>
    private static string Join(JsonNode? value, string separator) => value is JsonArray array ? string.Join(separator, array.Select(Text)) : Text(value);

    /// <summary>
    /// Normalizes a scalar into a one-item sequence or returns the non-null items of an array.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The sequence used by collection functions.</returns>
    private static IEnumerable<JsonNode> ToArray(JsonNode? value) => value is JsonArray array
        ? array.Where(item => item != null).Select(item => item!)
        : value == null ? Enumerable.Empty<JsonNode>() : new[] { value };
    /// <summary>
    /// Converts a scalar or array value to a cloned JSON array.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A new array containing cloned elements.</returns>
    private static JsonNode ToArrayNode(JsonNode? value) => new JsonArray(ToArray(value).Select(item => item.DeepClone()).ToArray());

    /// <summary>
    /// Concatenates arrays by flattening their elements, or concatenates scalar text values.
    /// </summary>
    /// <param name="values">The values to concatenate.</param>
    /// <returns>A cloned JSON array or concatenated JSON string.</returns>
    private static JsonNode Concat(IReadOnlyList<JsonNode?> values)
    {
        if (values.Any(value => value is JsonArray))
            return new JsonArray(values.SelectMany(ToArray).Select(item => item.DeepClone()).ToArray());
        return JsonValue.Create(string.Concat(values.Select(Text)));
    }
    /// <summary>
    /// Flattens one collection level while cloning each returned element.
    /// </summary>
    /// <param name="value">The scalar or array value to flatten.</param>
    /// <returns>A new JSON array containing the flattened elements.</returns>
    private static JsonNode Flatten(JsonNode? value) => new JsonArray(ToArray(value)
        .SelectMany(item => item is JsonArray nested ? nested.Where(child => child != null) : new[] { item })
        .Select(item => item.DeepClone()).ToArray());
    /// <summary>
    /// Returns the property names of a JSON object in enumeration order.
    /// </summary>
    /// <param name="value">The value expected to be a JSON object.</param>
    /// <returns>An array of property names, or an empty array for non-object values.</returns>
    private static JsonNode Keys(JsonNode? value) => value is JsonObject obj
        ? new JsonArray(obj.Select(item => JsonValue.Create(item.Key)).ToArray())
        : new JsonArray();
    /// <summary>
    /// Returns cloned property values of a JSON object in enumeration order.
    /// </summary>
    /// <param name="value">The value expected to be a JSON object.</param>
    /// <returns>An array of property values, or an empty array for non-object values.</returns>
    private static JsonNode Values(JsonNode? value) => value is JsonObject obj
        ? new JsonArray(obj.Select(item => item.Value).Where(item => item != null).Select(item => item!.DeepClone()).ToArray())
        : new JsonArray();
    /// <summary>
    /// Tests whether a JSON object contains a property with the requested name.
    /// </summary>
    /// <param name="value">The value expected to be a JSON object.</param>
    /// <param name="key">The property name to locate.</param>
    /// <returns>A JSON boolean indicating whether the property exists.</returns>
    private static JsonNode Has(JsonNode? value, JsonNode? key) => JsonValue.Create(value is JsonObject obj &&
        obj.Any(item => string.Equals(item.Key, Text(key), StringComparison.OrdinalIgnoreCase)));
    /// <summary>
    /// Returns a cloned array with the normalized sequence order reversed.
    /// </summary>
    /// <param name="value">The scalar or array value to reverse.</param>
    /// <returns>A new JSON array in reverse order.</returns>
    private static JsonNode Reverse(JsonNode? value) => new JsonArray(ToArray(value).Reverse().Select(item => item.DeepClone()).ToArray());

    /// <summary>
    /// Returns a bounded slice of the normalized sequence.
    /// </summary>
    /// <param name="value">The scalar or array value to slice.</param>
    /// <param name="skip">The number of elements to skip.</param>
    /// <param name="take">The maximum number of elements to return.</param>
    /// <returns>A new JSON array containing the selected elements.</returns>
    private static JsonNode Slice(JsonNode? value, int skip, int take) => new JsonArray(ToArray(value)
        .Skip(Math.Max(0, skip)).Take(Math.Max(0, take)).Select(item => item.DeepClone()).ToArray());
    /// <summary>
    /// Removes duplicate normalized values while preserving the first occurrence of each value.
    /// </summary>
    /// <param name="value">The scalar or array value to de-duplicate.</param>
    /// <returns>A new JSON array containing unique cloned values.</returns>
    private static JsonNode Unique(JsonNode? value) => new JsonArray(ToArray(value)
        .GroupBy(Text, StringComparer.Ordinal).Select(group => group.First().DeepClone()).ToArray());
    /// <summary>
    /// Sorts a JSON array by an optional dotted property path and ascending or descending direction.
    /// The original array is not modified.
    /// </summary>
    /// <param name="value">The value expected to be a JSON array.</param>
    /// <param name="path">The optional object property path used as the sort key.</param>
    /// <param name="direction">The sort direction, either <c>asc</c> or <c>desc</c>.</param>
    /// <returns>A cloned sorted array, or a cloned scalar value when the input is not an array.</returns>
    /// <exception cref="ExpressionException">Thrown when <paramref name="path"/> contains an invalid segment or <paramref name="direction"/> is not supported.</exception>
    private static JsonNode Sort(JsonNode? value, string path, string direction)
    {
        if (value is not JsonArray array) return value?.DeepClone() ?? JsonValue.Create(string.Empty);
        if (!string.IsNullOrWhiteSpace(path) && !path.Split('.').All(segment =>
                segment.Length > 0 && segment.All(character => char.IsLetterOrDigit(character) || character == '_')))
            throw new ExpressionException("sort/orderBy field paths may contain only letters, numbers, underscores, and dots.");
        if (!string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
            throw new ExpressionException("sort/orderBy direction must be asc or desc.");
        var items = array.Select((item, index) => new { Item = item, Index = index, Key = ReadPath(item, path) });
        var result = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase)
            ? items.OrderByDescending(item => SortKey(item.Key), StringComparer.Ordinal).ThenBy(item => item.Index)
            : items.OrderBy(item => SortKey(item.Key), StringComparer.Ordinal).ThenBy(item => item.Index);
        return new JsonArray(result.Select(item => item.Item?.DeepClone()).ToArray());
    }
    /// <summary>
    /// Reads a dotted property path from nested JSON objects.
    /// </summary>
    /// <param name="value">The root JSON value.</param>
    /// <param name="path">The dotted property path.</param>
    /// <returns>The final property value, or <see langword="null"/> when a segment is missing.</returns>
    private static JsonNode? ReadPath(JsonNode? value, string path)
    {
        var current = value;
        foreach (var segment in (path ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(segment, out current)) return null;
        }
        return current;
    }
    /// <summary>
    /// Produces a stable textual sort key, padding numeric values so numeric ordering
    /// remains distinct from ordinary lexical ordering.
    /// </summary>
    /// <param name="value">The value to convert into a sort key.</param>
    /// <returns>The invariant numeric key or the textual value.</returns>
    private static string SortKey(JsonNode? value) => decimal.TryParse(Text(value), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
        ? number.ToString("00000000000000000000000000000000.0000000000000000000000000000", CultureInfo.InvariantCulture)
        : Text(value);
    /// <summary>
    /// Converts a JSON value to expression text while preserving non-ASCII characters.
    /// String values are returned without JSON quoting; arrays and objects use their readable
    /// JSON representation.
    /// </summary>
    /// <param name="value">The JSON value to convert.</param>
    /// <returns>The expression text representation, or an empty string for <see langword="null"/>.</returns>
    private static string Text(JsonNode? value)
    {
        if (value == null) return string.Empty;
        if (value is JsonValue json && json.TryGetValue<string>(out var text)) return text ?? string.Empty;
        // Text functions operate on the readable JSON representation of arrays/objects.
        // Keep non-ASCII characters intact so upper/lower cannot transform JSON's "\u"
        // escape marker into the invalid "\U" form.
        return value.ToJsonString(TextJsonOptions);
    }

    private sealed class ExpressionException(string message) : Exception(message) { }
}
