/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TextJsonConverter.cs
    File description: TextJsonConverter related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261103
    Change description: v0.11.11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Senparc.Xncf.Dapr.Utils.Serializer
{
    public static class TextJsonConverter
    {
        public class DateTimeParse : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return ParseString(ref reader, value => DateTime.Parse(value, CultureInfo.InvariantCulture));
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }
        public class IntParse : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
                {
                    return value;
                }
                return ParseString(ref reader, value => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
        public class DecimalParse : JsonConverter<decimal>
        {
            public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var value))
                {
                    return value;
                }
                return ParseString(ref reader, value => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture));
            }

            public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
        public class DoubleParse : JsonConverter<double>
        {
            public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var value))
                {
                    return value;
                }
                return ParseString(ref reader, value => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
            }

            public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
        public class FloatParse : JsonConverter<float>
        {
            public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetSingle(out var value))
                {
                    return value;
                }
                return ParseString(ref reader, value => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
            }

            public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
        public class GuidParse : JsonConverter<Guid>
        {
            public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String && reader.TryGetGuid(out var value))
                {
                    return value;
                }
                throw CreateJsonException(reader, typeof(Guid));
            }

            public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }
        }
        public class BoolParse : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                {
                    return reader.GetBoolean();
                }
                return ParseString(ref reader, bool.Parse);
            }

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
                writer.WriteBooleanValue(value);
            }
        }
        /// <summary>
        /// Converts a JSON string token with the supplied parser and translates format
        /// or overflow failures into a <see cref="JsonException"/>.
        /// </summary>
        /// <typeparam name="T">The target primitive type.</typeparam>
        /// <param name="reader">The JSON reader positioned at the value being converted.</param>
        /// <param name="parser">The parser that converts the non-empty token text.</param>
        /// <returns>The parsed primitive value.</returns>
        /// <exception cref="JsonException">Thrown when the token is not a non-empty string or cannot be parsed.</exception>
        private static T ParseString<T>(ref Utf8JsonReader reader, Func<string, T> parser)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw CreateJsonException(reader, typeof(T));
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw CreateJsonException(reader, typeof(T));
            }

            try
            {
                return parser(value!);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new JsonException($"Value '{value}' is not a valid {typeof(T).Name}.", ex);
            }
        }

        /// <summary>
        /// Creates a consistent JSON conversion exception for an unexpected token type.
        /// </summary>
        /// <param name="reader">The reader containing the unexpected token.</param>
        /// <param name="targetType">The type the token was expected to represent.</param>
        /// <returns>A <see cref="JsonException"/> describing the failed conversion.</returns>
        private static JsonException CreateJsonException(Utf8JsonReader reader, Type targetType)
        {
            return new JsonException($"Token {reader.TokenType} cannot be converted to {targetType.Name}.");
        }
    }

}
