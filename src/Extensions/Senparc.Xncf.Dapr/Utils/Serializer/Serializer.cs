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
            options.PropertyNameCaseInsensitive = true;//Ignore case
            //Basic type handling through client-side custom overloading
            options.Converters.Add(new TextJsonConverter.DateTimeParse());
            options.Converters.Add(new TextJsonConverter.IntParse());
            options.Converters.Add(new TextJsonConverter.DoubleParse());
            options.Converters.Add(new TextJsonConverter.DecimalParse());
            options.Converters.Add(new TextJsonConverter.FloatParse());
            options.Converters.Add(new TextJsonConverter.GuidParse());
            options.Converters.Add(new TextJsonConverter.BoolParse());
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; //Respond to camelCase naming
            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;//Chinese garbled characters
            options.AllowTrailingCommas = true;//Allow extra commas at end of array
            return options;
        });

        public Serializer(ILogger<Serializer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Serialize T to JSON string
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
                _logger.LogError($"Failed to serialize object:{e.Message}");
            }
            return default;
        }

        /// <summary>
        /// Deserialize JSON string to T
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
                _logger.LogError($"Deserializing object failed:{e.Message}，Message body:{input}");
            }
            return default;
        }

        /// <summary>
        /// Serialize JSON string to object
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
                _logger.LogError($"Deserializing object failed:{e.Message}，Message body:{input}");
            }
            return default;
        }
    }
}
