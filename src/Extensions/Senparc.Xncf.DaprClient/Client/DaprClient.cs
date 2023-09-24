using Microsoft.Extensions.Logging;
using Senparc.Xncf.DaprClient.Blocks.ServiceInvoke;
using Senparc.Xncf.DaprClient.Blocks.StateStore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Senparc.Xncf.DaprClient.Client
{
    public class DaprClient
    {
        private readonly HttpClient _httpClient;
        private readonly Logger<DaprClient> _logger;
        public static DaprClientConfigOptions options = new() { HttpApiPort = 3500, DaprConnectionRetryCount=3 };//使用默认的Api端口
        public DaprClient(HttpClient httpClient, Logger<DaprClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        /// <summary>
        /// 服务调用
        /// </summary>
        /// <typeparam name="TResult">返回的数据类型</typeparam>
        /// <param name="invokeType">请求类型</param>
        /// <param name="serviceId">服务名称 (App-Id)</param>
        /// <param name="methodName">方法路径</param>
        /// <param name="data">请求数据</param>
        /// <param name="options">json序列化选项</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<TResult> InvokeMethodAsync<TResult>(InvokeType invokeType, string serviceId, string methodName, object? data = null, JsonSerializerOptions? options = default)
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
            var request = BuildMessage(messageType, serviceId, methodName, data);

            var response = await SendMessageAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            TResult result;
            try
            {
                result = JsonSerializer.Deserialize<TResult>(json, options);
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="topicName">主题名称</param>
        /// <param name="data">发布内容</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task PublishEventAsync(string topicName, object data)
        {
            if(options.PubSubName == null)
                throw new Exception("没有配置全局PubSubName");
            await PublishEventAsync(options.PubSubName, topicName, data);
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="pubSubName">发布订阅组件名称</param>
        /// <param name="topicName">主题名称</param>
        /// <param name="data">发布数据</param>
        /// <returns></returns>
        public async Task PublishEventAsync(string pubSubName, string topicName, object data)
        {
            var request = BuildMessage(MessageType.Publish, pubSubName, topicName, data);
            await SendMessageAsync(request);
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        /// <typeparam name="TResult">返回状态类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<object> GetStateAsync<TResult>(string key)
        {
            if (options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            return await GetStateAsync<TResult>(options.StateStoreName, key);
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        /// <typeparam name="TResult">返回状态类型</typeparam>
        /// <param name="stateStore">状态存储组件名称</param>
        /// <param name="key">缓存键</param>
        /// <returns></returns>
        public async Task<object> GetStateAsync<TResult>(string stateStore, string key)
        {
            var request = BuildMessage(MessageType.GetState, stateStore, key);
            return await _httpClient.SendAsync(request);
        }
        /// <summary>
        /// 保存状态
        /// </summary>
        /// <typeparam name="TValue">状态类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="data">缓存数据</param>
        /// <param name="ttl">缓存生命周期</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SetStateAsync<TValue>(string key, TValue data, int ttl = -1)
        {
            if (options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            await SetStateAsync<TValue>(options.StateStoreName, key, data);
        }
        /// <summary>
        /// 保存状态
        /// </summary>
        /// <typeparam name="TValue">状态类型</typeparam>
        /// <param name="stateStore">状态存储组件名称</param>
        /// <param name="key">缓存键</param>
        /// <param name="data">缓存数据</param>
        /// <param name="ttl">缓存生命周期</param>
        /// <returns></returns>
        public async Task SetStateAsync<TValue>(string stateStore, string key, TValue data, int ttl = -1)
        {
            var state = new StateStore(key, data, ttl);
            var request = BuildMessage(MessageType.SetState, stateStore, key, state);
            await _httpClient.SendAsync(request);
        }
        /// <summary>
        /// 删除状态
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task DelStateAsync(string key)
        {
            if (options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            await DelStateAsync(options.StateStoreName, key);
        }
        /// <summary>
        /// 删除状态
        /// </summary>
        /// <param name="stateStore">状态存储组件名称</param>
        /// <param name="key">缓存键</param>
        /// <returns></returns>
        public async Task DelStateAsync(string stateStore, string key)
        {
            var request = BuildMessage(MessageType.DeleteState, stateStore, key);
            await _httpClient.SendAsync(request);
        }
        /// <summary>
        /// 确认Dapr Sidecar是否正常运行
        /// </summary>
        /// <returns></returns>
        public async Task<bool> HealthCheckAsync()
        {
            int retryCount = 0;
            bool isHealth = false;
            if (!isHealth)
            {
                while (retryCount < options.DaprConnectionRetryCount)
                {
                    try
                    {
                        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:3500/v1.0/healthz"));
                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            isHealth = true;
                            break;
                        }
                        else
                        {
                            _logger.LogWarning($"Dapr尚未准备就绪!");
                            await Task.Delay(100);
                        }                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Dapr尚未准备就绪,{e.Message}");
                        break;
                    }
                }
            }
            return isHealth;
        }

        /// <summary>
        /// 使用Http客户端发布消息
        /// </summary>
        /// <param name="message">Http请求消息</param>
        /// <returns>Http返回消息</returns>
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
