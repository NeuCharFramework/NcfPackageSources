using Microsoft.Extensions.Logging;
using Senparc.Xncf.Dapr.Blocks.HealthCheck.Interface;
using Senparc.Xncf.Dapr.Blocks.PubSub.Interface;
using Senparc.Xncf.Dapr.Blocks.ServiceInvoke;
using Senparc.Xncf.Dapr.Blocks.ServiceInvoke.Interface;
using Senparc.Xncf.Dapr.Blocks.StateManage;
using Senparc.Xncf.Dapr.Blocks.StateManage.Interface;
using Senparc.Xncf.Dapr.Utils.Serializer;
using System.Net;
using System.Text;

namespace Senparc.Xncf.Dapr
{
    //TODO: 返回DaprAPI文档中提供的状态码信息
    public class DaprClient : IServiceInvoke, IEventPublish, IStateManage, IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DaprClient> _logger;
        private readonly ISerializer _serializer;
        public static DaprClientOptions options = new() { ApiPort = 3500, DaprConnectionRetryCount = 3 };//使用默认的Api端口
        public DaprClient(HttpClient httpClient, ILogger<DaprClient> logger, ISerializer serializer)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serializer = serializer;
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
        public async Task<TResult> InvokeMethodAsync<TResult>(InvokeType invokeType, string serviceId, string methodName, object? data = null)
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
                result = _serializer.DeserializesJson<TResult>(json);
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }

        public async Task<TResult> PatchAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Patch, serviceId, methodName, data);

        public async Task<TResult> GetAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Get, serviceId, methodName, data);

        public async Task<TResult> PostAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Post, serviceId, methodName, data);

        public async Task<TResult> PutAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Put, serviceId, methodName, data);

        public async Task<TResult> DeleteAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Delete, serviceId, methodName, data);

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
        public async Task<TResult> GetStateAsync<TResult>(string key)
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
        public async Task<TResult> GetStateAsync<TResult>(string stateStore, string key)
        {
            var request = BuildMessage(MessageType.GetState, stateStore, key);
            var response = await SendMessageAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            TResult result;
            try
            {
                result = _serializer.DeserializesJson<TResult>(json);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }

        /// <summary>
        /// 保存状态
        /// </summary>
        /// <typeparam name="TValue">状态类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存数据</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SetStateAsync<TValue>(string key, TValue value)
        {
            if (options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            await SetStateAsync<TValue>(options.StateStoreName, key, value);
        }

        /// <summary>
        /// 保存状态
        /// </summary>
        /// <typeparam name="TValue">状态类型</typeparam>
        /// <param name="stateStore">状态存储组件名称</param>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存数据</param>
        /// <returns></returns>
        public async Task SetStateAsync<TValue>(string stateStore, string key, TValue value)
        {
            var state = new StateStore(key, value);
            await SetStatesAsync(stateStore, new List<StateStore> { state });
        }

        /// <summary>
        /// 保存多个状态
        /// </summary>
        /// <typeparam name="TValue">状态类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="values">缓存数据</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SetStatesAsync(IEnumerable<StateStore> values)
        {
            if (options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            await SetStatesAsync(options.StateStoreName, values);
        }

        /// <summary>
        /// 保存多个状态
        /// </summary>
        /// <typeparam name="TValue">状态类型</typeparam>
        /// <param name="stateStore">状态存储组件名称</param>
        /// <param name="key">缓存键</param>
        /// <param name="values">缓存数据</param>
        /// <returns></returns>
        public async Task SetStatesAsync(string stateStore, IEnumerable<StateStore> values)
        {
            var request = BuildMessage(MessageType.SetState, stateStore, "", values);
            await SendMessageAsync(request);
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
                        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{options.ApiPort}/v1.0/healthz"));
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
            var result = await _httpClient.SendAsync(message);
            return result;
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
            string bathUrl = $"http://localhost:{options.ApiPort}";
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
                    url = $"{bathUrl}/{version}/state/{host}";
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
                //request.Content.Headers.ContentType = new MediaTypeHeaderValue($"application/json");
                string json = _serializer.SerializesJson(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return request;
        }
    }
}
