/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Serializer.cs
    File description: Serializer related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Senparc.Xncf.Dapr.Utils.Serializer
{
    internal class Serializer : ISerializer
    {
        private readonly ILogger<Serializer> _logger;
        public static Lazy<JsonSerializerOptions> JsonSerializerOptions = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;//Legacy comment translated to English.
            //Legacy comment translated to English.
            options.Converters.Add(new TextJsonConverter.DateTimeParse());
            options.Converters.Add(new TextJsonConverter.IntParse());
            options.Converters.Add(new TextJsonConverter.DoubleParse());
            options.Converters.Add(new TextJsonConverter.DecimalParse());
            options.Converters.Add(new TextJsonConverter.FloatParse());
            options.Converters.Add(new TextJsonConverter.GuidParse());
            options.Converters.Add(new TextJsonConverter.BoolParse());
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; //Legacy comment translated to English.
            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;//Legacy comment translated to English.
            options.AllowTrailingCommas = true;//Legacy comment translated to English.
            return options;
        });

        public Serializer(ILogger<Serializer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public string SerializesJson<T>(T input, bool IngoreOptions = false)
        {
            if (input == null)
                return default;
            try
            {
                if (IngoreOptions)
                    return JsonSerializer.Serialize(input);
                else
                    return JsonSerializer.Serialize(input, JsonSerializerOptions.Value);
            }
            catch (Exception e)
            {
                _logger.LogError($"序列化对象失败：{e.Message}");
            }
            return default;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public T DeserializesJson<T>(string input)
        {
            if (input == null || !input.Any())
                return default;
            try
            {
                return JsonSerializer.Deserialize<T>(input, JsonSerializerOptions.Value);
            }
            catch (Exception e)
            {
                _logger.LogError($"反序化对象失败：{e.Message}，消息体：{input}");
            }
            return default;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public object DeserializesJson(Type type, string input)
        {
            if (input == null || !input.Any())
                return default;
            try
            {
                return JsonSerializer.Deserialize(input, type, JsonSerializerOptions.Value);
            }
            catch (Exception e)
            {
                _logger.LogError($"反序化对象失败：{e.Message}，消息体：{input}");
            }
            return default;
        }
    }
}
