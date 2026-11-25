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
            options.PropertyNameCaseInsensitive = true;
            // Accept the primitive representations emitted by the Dapr integration.
            options.Converters.Add(new TextJsonConverter.DateTimeParse());
            options.Converters.Add(new TextJsonConverter.IntParse());
            options.Converters.Add(new TextJsonConverter.DoubleParse());
            options.Converters.Add(new TextJsonConverter.DecimalParse());
            options.Converters.Add(new TextJsonConverter.FloatParse());
            options.Converters.Add(new TextJsonConverter.GuidParse());
            options.Converters.Add(new TextJsonConverter.BoolParse());
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.AllowTrailingCommas = true;
            return options;
        });

        /// <summary>
        /// Initializes the Dapr serializer with an error logger.
        /// </summary>
        /// <param name="logger">The logger used when serialization or deserialization fails.</param>
        public Serializer(ILogger<Serializer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Serializes an object to JSON using either the shared Dapr serializer options
        /// or the default <see cref="JsonSerializer"/> options when explicitly requested.
        /// Serialization failures are logged and represented by the default value for
        /// <see cref="string"/>.
        /// </summary>
        /// <typeparam name="T">The runtime type of the value being serialized.</typeparam>
        /// <param name="input">The value to serialize. A <see langword="null"/> value produces the default string value.</param>
        /// <param name="IngoreOptions">When <see langword="true"/>, bypasses the module's custom converters and naming options.</param>
        /// <returns>The serialized JSON text, or the default string value when serialization fails.</returns>
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
        /// Deserializes JSON text into the requested type using the module's shared converter set.
        /// Empty input and conversion failures are logged and return the default value for
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to which the JSON payload is converted.</typeparam>
        /// <param name="input">The JSON payload to deserialize.</param>
        /// <returns>The deserialized value, or the default value when the input is empty or invalid.</returns>
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
        /// Deserializes JSON text into a runtime-supplied target type using the module's
        /// shared converter set.
        /// </summary>
        /// <param name="type">The runtime type to which the JSON payload is converted.</param>
        /// <param name="input">The JSON payload to deserialize.</param>
        /// <returns>The deserialized object, or <see langword="null"/> when the input is empty or invalid.</returns>
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
