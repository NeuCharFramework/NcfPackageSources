/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FunctionRequestParameterNormalizer.cs
    文件功能描述：FunctionRequestParameterNormalizer 相关实现
    
    
    创建标识：Senparc - 20260424
    
    修改标识：Senparc - 20260704
    修改描述：v0.23.2-preview1 增强函数请求参数归一化与占位符处理能力

----------------------------------------------------------------*/
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    /// <summary>
    /// Normalizes request payloads so legacy SelectionList JSON and simple string JSON
    /// can both bind to FunctionRender request types.
    /// </summary>
    public static class FunctionRequestParameterNormalizer
    {
        public static string NormalizeJson(string rawJson, Type requestType)
        {
            if (string.IsNullOrWhiteSpace(rawJson) || requestType == null)
            {
                return rawJson;
            }

            JsonNode rootNode;
            try
            {
                rootNode = JsonNode.Parse(rawJson);
            }
            catch
            {
                return rawJson;
            }

            if (rootNode is not JsonObject jsonObject)
            {
                return rawJson;
            }

            var properties = requestType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                        .Where(z => z.CanWrite)
                                        .ToList();

            foreach (var property in properties)
            {
                if (!TryGetJsonProperty(jsonObject, property.Name, out var jsonKey, out var currentNode))
                {
                    continue;
                }

                if (property.PropertyType == typeof(SelectionList))
                {
                    jsonObject[jsonKey] = NormalizeSelectionListNode(currentNode);
                    continue;
                }

                var uiAttr = property.GetCustomAttribute<FunctionParameterUiAttribute>();
                jsonObject[jsonKey] = NormalizeSimpleNode(property.PropertyType, currentNode, uiAttr);
            }

            return jsonObject.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        private static JsonNode NormalizeSelectionListNode(JsonNode currentNode)
        {
            if (currentNode is JsonObject currentObject && TryGetJsonProperty(currentObject, "SelectedValues", out _, out _))
            {
                return currentNode;
            }

            var values = ExtractValues(currentNode, forMultiple: true);
            var selectedValues = new JsonArray(values.Select(z => JsonValue.Create(z)).ToArray());

            return new JsonObject
            {
                ["SelectedValues"] = selectedValues
            };
        }

        private static JsonNode NormalizeSimpleNode(Type targetType, JsonNode currentNode, FunctionParameterUiAttribute uiAttr)
        {
            if (IsBooleanTargetType(targetType) && TryNormalizeBooleanJsonNode(currentNode, out var boolJson))
            {
                return boolJson;
            }

            var forMultiple = uiAttr?.ParameterType == ParameterType.CheckBoxList || targetType == typeof(string[]);
            var values = ExtractValues(currentNode, forMultiple);

            if (targetType == typeof(string[]))
            {
                return new JsonArray(values.Select(z => JsonValue.Create(z)).ToArray());
            }

            if (targetType == typeof(string))
            {
                if (currentNode is JsonValue valueNode && TryGetString(valueNode, out var stringValue))
                {
                    return JsonValue.Create(stringValue);
                }

                return JsonValue.Create(forMultiple ? string.Join("\n", values) : values.FirstOrDefault());
            }

            var firstValue = values.FirstOrDefault();
            if (firstValue == null)
            {
                return null;
            }

            if (IsNullableType(targetType, typeof(int)) && int.TryParse(firstValue, out var intValue))
            {
                return JsonValue.Create(intValue);
            }

            if (IsNullableType(targetType, typeof(long)) && long.TryParse(firstValue, out var longValue))
            {
                return JsonValue.Create(longValue);
            }

            if (IsNullableType(targetType, typeof(bool)) && bool.TryParse(firstValue, out var boolValue))
            {
                return JsonValue.Create(boolValue);
            }

            return JsonValue.Create(firstValue);
        }

        private static List<string> ExtractValues(JsonNode currentNode, bool forMultiple)
        {
            if (currentNode == null)
            {
                return new List<string>();
            }

            if (currentNode is JsonObject currentObject)
            {
                if (TryGetJsonProperty(currentObject, "SelectedValues", out _, out var selectedValuesNode))
                {
                    return ExtractValues(selectedValuesNode, true);
                }

                return new List<string>();
            }

            if (currentNode is JsonArray currentArray)
            {
                return currentArray.SelectMany(z => ExtractValues(z, true))
                                   .Where(z => !string.IsNullOrWhiteSpace(z))
                                   .Distinct(StringComparer.Ordinal)
                                   .ToList();
            }

            if (currentNode is JsonValue currentValue && TryGetString(currentValue, out var stringValue))
            {
                return SplitInput(stringValue, forMultiple);
            }

            return new List<string>();
        }

        private static List<string> SplitInput(string input, bool forMultiple)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<string>();
            }

            if (!forMultiple)
            {
                return new List<string> { input.Trim() };
            }

            return input.Split(new[] { ',', '，', ';', '；', '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(z => z.Trim())
                        .Where(z => !string.IsNullOrWhiteSpace(z))
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
        }

        private static bool TryGetJsonProperty(JsonObject jsonObject, string propertyName, out string actualKey, out JsonNode currentNode)
        {
            actualKey = null;
            currentNode = null;

            foreach (var item in jsonObject)
            {
                if (string.Equals(item.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    actualKey = item.Key;
                    currentNode = item.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetString(JsonValue jsonValue, out string result)
        {
            result = null;

            try
            {
                result = jsonValue.GetValue<string>();
                return true;
            }
            catch
            {
                try
                {
                    result = jsonValue.ToString();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static bool IsNullableType(Type actualType, Type targetType)
        {
            var underlyingType = Nullable.GetUnderlyingType(actualType) ?? actualType;
            return underlyingType == targetType;
        }

        private static bool IsBooleanTargetType(Type targetType)
        {
            return targetType == typeof(bool) || Nullable.GetUnderlyingType(targetType) == typeof(bool);
        }

        private static bool TryNormalizeBooleanJsonNode(JsonNode currentNode, out JsonNode boolJson)
        {
            boolJson = null;
            if (!TryGetBooleanValue(currentNode, out var parsedValue))
            {
                return false;
            }

            boolJson = JsonValue.Create(parsedValue);
            return true;
        }

        private static bool TryGetBooleanValue(JsonNode currentNode, out bool result)
        {
            result = false;

            if (currentNode == null)
            {
                return false;
            }

            if (currentNode is JsonValue jsonValue)
            {
                try
                {
                    if (jsonValue.TryGetValue<bool>(out var direct))
                    {
                        result = direct;
                        return true;
                    }
                }
                catch
                {
                    // fall through
                }

                if (TryGetString(jsonValue, out var token) && TryParseBooleanToken(token, out var parsed))
                {
                    result = parsed;
                    return true;
                }

                return false;
            }

            if (currentNode is JsonArray or JsonObject)
            {
                var values = ExtractValues(currentNode, true);
                if (values.Count == 0)
                {
                    result = false;
                    return true;
                }

                var hasParsedBoolean = false;
                foreach (var value in values)
                {
                    if (!TryParseBooleanToken(value, out var parsed))
                    {
                        continue;
                    }

                    hasParsedBoolean = true;
                    if (parsed)
                    {
                        result = true;
                        return true;
                    }
                }

                if (hasParsedBoolean)
                {
                    result = false;
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseBooleanToken(string token, out bool result)
        {
            result = false;
            if (token == null)
            {
                return false;
            }

            var normalized = token.Trim();
            if (normalized.Length == 0)
            {
                result = false;
                return true;
            }

            if (bool.TryParse(normalized, out var boolValue))
            {
                result = boolValue;
                return true;
            }

            switch (normalized.ToLowerInvariant())
            {
                case "1":
                case "yes":
                case "y":
                case "on":
                    result = true;
                    return true;
                case "0":
                case "no":
                case "n":
                case "off":
                    result = false;
                    return true;
                default:
                    return false;
            }
        }
    }
}
