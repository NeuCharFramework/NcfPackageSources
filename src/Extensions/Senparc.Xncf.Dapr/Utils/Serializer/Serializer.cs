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
            options.PropertyNameCaseInsensitive = true;//忽略大小写
            //基础类型处理通过客户端自定义重载
            options.Converters.Add(new TextJsonConverter.DateTimeParse());
            options.Converters.Add(new TextJsonConverter.IntParse());
            options.Converters.Add(new TextJsonConverter.DoubleParse());
            options.Converters.Add(new TextJsonConverter.DecimalParse());
            options.Converters.Add(new TextJsonConverter.FloatParse());
            options.Converters.Add(new TextJsonConverter.GuidParse());
            options.Converters.Add(new TextJsonConverter.BoolParse());
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; //响应驼峰命名
            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;//中文乱码
            options.AllowTrailingCommas = true;//允许数组末尾多余的逗号
            return options;
        });

        public Serializer(ILogger<Serializer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 序列化T为JSON字符串
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
        /// 反序列化JSON字符串为T
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
        /// 序列化JSON字符串为object
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
