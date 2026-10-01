/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TextJsonConverter.cs
    文件功能描述：TextJsonConverter 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

        private static JsonException CreateJsonException(Utf8JsonReader reader, Type targetType)
        {
            return new JsonException($"Token {reader.TokenType} cannot be converted to {targetType.Name}.");
        }
    }

}
