using Senparc.Xncf.DaprClient.Blocks.ServiceInvoke;
using Senparc.Xncf.DaprClient.Blocks.StateStore;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Json;

namespace Senparc.Xncf.DaprClient.Client
{
    public class DaprClient
    {
        private readonly HttpClient _httpClient;
        public static DaprClientConfigOptions options = new() { HttpApiPort = 3500 };//使用默认的Api端口
        public DaprClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        /// <summary>
        /// 服务调用
        /// </summary>
        /// <typeparam name="TResult">返回的数据类型</typeparam>
        /// <param name="invokeType">请求类型</param>
        /// <param name="host">服务名称 (App-Id)</param>
        /// <param name="path">方法路径</param>
        /// <param name="data">请求数据</param>
        /// <param name="options">json序列化选项</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<TResult> InvokeMethodAsync<TResult>(InvokeType invokeType, string host, string path, object? data = null, JsonSerializerOptions? options = default)
        {
            MessageType messageType;
            switch (invokeType)
            {
                case InvokeType.Patch:
                    messageType = MessageType.InvokePatch; break;
                case InvokeType.Get:
                    messageType = MessageType.InvokeGet; break;
                case InvokeType.Post:
                    messageType = MessageType.InvokePost; break;
                case InvokeType.Put:
                    messageType = MessageType.InvokePut; break;
                case InvokeType.Delete:
                    messageType = MessageType.InvokeDelete; break;
                default:
                    throw new ArgumentException();
            }
            var request = BuildMessage(messageType, host, path, data);

            var response = await SendMessageAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TResult>(json, options);
            return result;
        }

        public async Task PublishEventAsync(string pubSubName, string topicName, object data)
        {
            throw new NotImplementedException();
        }

        public async Task<object> GetStateAsync<T>(string host, string key)
        {
            var request = BuildMessage(MessageType.GetState, host, key);
            return await _httpClient.SendAsync(request);
        }

        public async Task SetStateAsync<TValue>(string host, string key, TValue data, int ttl = -1)
        {
            var state = new StateStore(key, data, ttl);
            var request = BuildMessage(MessageType.SetState, host, key, data);
        }

        public async Task DelStateAsync(string key)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HealthCheckAsync()
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:3500/v1.0/healthz"));
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<HttpResponseMessage> SendMessageAsync(HttpRequestMessage message)
        {
            return await _httpClient.SendAsync(message);
        }
        /// <summary>
        /// 构建请求消息
        /// </summary>
        /// <param name="requestType">调用的Dapr API类型</param>
        /// <param name="host">服务名称 (App-Id)</param>
        /// <param name="path">资源路径</param>
        /// <param name="data">承载数据</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private HttpRequestMessage BuildMessage(MessageType requestType, string host, string path, object? data = null)
        {
            string bathUrl = $"http://localhost:{options.HttpApiPort}";
            string version = "v1.0";
            string url;

            HttpRequestMessage request;
            //构建请求URL
            switch (requestType)
            {
                case MessageType.InvokePatch:
                    url = $"{bathUrl}/{version}/invoke/{host}/method/{path}";
                    request = new(HttpMethod.Patch, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.InvokeGet:
                    url = $"{bathUrl}/{version}/invoke/{host}/method/{path}";
                    request = new(HttpMethod.Get, url) { Version = new Version(1, 1) };

                    break;
                case MessageType.InvokePost:
                    url = $"{bathUrl}/{version}/invoke/{host}/method/{path}";
                    request = new(HttpMethod.Post, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.InvokePut:
                    url = $"{bathUrl}/{version}/invoke/{host}/method/{path}";
                    request = new(HttpMethod.Put, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.InvokeDelete:
                    url = $"{bathUrl}/{version}/invoke/{host}/method/{path}";
                    request = new(HttpMethod.Delete, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.Publish:
                    url = $"{bathUrl}/{version}/publish/{host}/{path}";
                    request = new(HttpMethod.Post, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.SetState:
                    url = $"{bathUrl}/{version}/state/{host}/{path}";
                    request = new(HttpMethod.Post, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.GetState:
                    url = $"{bathUrl}/{version}/state/{host}/{path}";
                    request = new(HttpMethod.Get, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.DeleteState:
                    url = $"{bathUrl}/{version}/state/{host}/{path}";
                    request = new(HttpMethod.Delete, url) { Version = new Version(1, 1) };
                    break;
                default:
                    throw new ArgumentException();
            }
            //构建请求体
            if (data != null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue($"application/json");
                string json = JsonSerializer.Serialize(data);
                request.Content = new StringContent(json);
            }

            return request;
        }
    }
}
