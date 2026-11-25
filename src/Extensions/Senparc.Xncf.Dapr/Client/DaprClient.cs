/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DaprClient.cs
    File description: DaprClient related implementation

    Created by: Senparc - 20230920

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Models.DataBaseModel;
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
    public class DaprClient : IServiceInvoke, IEventPublish, IStateManage, IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DaprClient> _logger;
        private readonly ISerializer _serializer;
        private readonly DaprClientOptions _options;
        /// <summary>
        /// Initializes a client that communicates with the local Dapr sidecar.
        /// </summary>
        /// <param name="httpClient">HTTP client used to send requests to the sidecar.</param>
        /// <param name="logger">Logger used to record sidecar response and retry information.</param>
        /// <param name="serializer">Serializer used for request and response payloads.</param>
        /// <param name="options">Dapr endpoint and component configuration.</param>
        public DaprClient(HttpClient httpClient, ILogger<DaprClient> logger, ISerializer serializer, IOptions<DaprClientOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serializer = serializer;
            _options = options.Value;
        }

        /// <summary>
        /// Invokes a method exposed by another Dapr application through the local Dapr sidecar.
        /// The HTTP verb is selected from <paramref name="invokeType"/>, the target service is
        /// addressed by <paramref name="serviceId"/>, and <paramref name="data"/> is serialized
        /// as the request body when it is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TResult">The type used to deserialize the response body.</typeparam>
        /// <param name="invokeType">The invocation operation to perform.</param>
        /// <param name="serviceId">The Dapr application identifier that owns the target method.</param>
        /// <param name="methodName">The method route exposed by the target Dapr application.</param>
        /// <param name="data">The optional request payload serialized by the configured <see cref="ISerializer"/>.</param>
        /// <returns>A task that completes with the deserialized response value.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="invokeType"/> is not supported.</exception>
        /// <exception cref="Exception">Thrown when the response body cannot be deserialized as <typeparamref name="TResult"/>.</exception>
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

            if (response.StatusCode == HttpStatusCode.NotFound)
                _logger.LogError("未提供的方法名称");
            else if (response.StatusCode == HttpStatusCode.Forbidden)
                _logger.LogError("访问控制禁止调用");
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
                _logger.LogError("请求失败");

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

        /// <summary>
        /// Invokes a Dapr method using the HTTP PATCH operation.
        /// </summary>
        /// <typeparam name="TResult">The type used to deserialize the response body.</typeparam>
        /// <param name="serviceId">The Dapr application identifier that owns the target method.</param>
        /// <param name="methodName">The method route exposed by the target application.</param>
        /// <param name="data">The optional request payload.</param>
        /// <returns>A task that completes with the deserialized response value.</returns>
        public async Task<TResult> PatchAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Patch, serviceId, methodName, data);

        /// <summary>
        /// Invokes a Dapr method using the HTTP GET operation.
        /// </summary>
        /// <typeparam name="TResult">The type used to deserialize the response body.</typeparam>
        /// <param name="serviceId">The Dapr application identifier that owns the target method.</param>
        /// <param name="methodName">The method route exposed by the target application.</param>
        /// <param name="data">The optional request payload.</param>
        /// <returns>A task that completes with the deserialized response value.</returns>
        public async Task<TResult> GetAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Get, serviceId, methodName, data);

        /// <summary>
        /// Invokes a Dapr method using the HTTP POST operation.
        /// </summary>
        /// <typeparam name="TResult">The type used to deserialize the response body.</typeparam>
        /// <param name="serviceId">The Dapr application identifier that owns the target method.</param>
        /// <param name="methodName">The method route exposed by the target application.</param>
        /// <param name="data">The optional request payload.</param>
        /// <returns>A task that completes with the deserialized response value.</returns>
        public async Task<TResult> PostAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Post, serviceId, methodName, data);

        /// <summary>
        /// Invokes a Dapr method using the HTTP PUT operation.
        /// </summary>
        /// <typeparam name="TResult">The type used to deserialize the response body.</typeparam>
        /// <param name="serviceId">The Dapr application identifier that owns the target method.</param>
        /// <param name="methodName">The method route exposed by the target application.</param>
        /// <param name="data">The optional request payload.</param>
        /// <returns>A task that completes with the deserialized response value.</returns>
        public async Task<TResult> PutAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Put, serviceId, methodName, data);

        /// <summary>
        /// Invokes a Dapr method using the HTTP DELETE operation.
        /// </summary>
        /// <typeparam name="TResult">The type used to deserialize the response body.</typeparam>
        /// <param name="serviceId">The Dapr application identifier that owns the target method.</param>
        /// <param name="methodName">The method route exposed by the target application.</param>
        /// <param name="data">The optional request payload.</param>
        /// <returns>A task that completes with the deserialized response value.</returns>
        public async Task<TResult> DeleteAsync<TResult>(string serviceId, string methodName, object? data = null) =>
            await InvokeMethodAsync<TResult>(InvokeType.Delete, serviceId, methodName, data);

        /// <summary>
        /// Publishes an event through the globally configured Dapr pub/sub component.
        /// </summary>
        /// <param name="topicName">The pub/sub topic to which the event is published.</param>
        /// <param name="data">The event payload to serialize and publish.</param>
        /// <exception cref="Exception">Thrown when no global pub/sub component name has been configured.</exception>
        public async Task PublishEventAsync(string topicName, object data)
        {
            if(_options.PubSubName == null)
                throw new Exception("没有配置全局PubSubName");
            await PublishEventAsync(_options.PubSubName, topicName, data);
        }

        /// <summary>
        /// Publishes an event through a named Dapr pub/sub component.
        /// </summary>
        /// <param name="pubSubName">The configured Dapr pub/sub component name.</param>
        /// <param name="topicName">The topic to which the event is published.</param>
        /// <param name="data">The event payload to serialize and publish.</param>
        /// <exception cref="HttpRequestException">Thrown when the Dapr sidecar cannot be reached.</exception>
        public async Task PublishEventAsync(string pubSubName, string topicName, object data)
        {
            var request = BuildMessage(MessageType.Publish, pubSubName, topicName, data);
            var response = await SendMessageAsync(request);
        }

        /// <summary>
        /// Retrieves a state value from the globally configured Dapr state store.
        /// </summary>
        /// <typeparam name="TResult">The type used to deserialize the stored state value.</typeparam>
        /// <param name="key">The state key to retrieve.</param>
        /// <returns>A task that completes with the deserialized state value.</returns>
        /// <exception cref="Exception">Thrown when no global state store name has been configured or the response cannot be deserialized.</exception>
        public async Task<TResult> GetStateAsync<TResult>(string key)
        {
            if (_options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            return await GetStateAsync<TResult>(_options.StateStoreName, key);
        }

        /// <summary>
        /// Retrieves a state value from a named Dapr state store.
        /// </summary>
        /// <typeparam name="TResult">The type used to deserialize the stored state value.</typeparam>
        /// <param name="stateStore">The configured Dapr state store component name.</param>
        /// <param name="key">The state key to retrieve.</param>
        /// <returns>A task that completes with the deserialized state value.</returns>
        /// <exception cref="Exception">Thrown when the response body cannot be deserialized as <typeparamref name="TResult"/>.</exception>
        public async Task<TResult> GetStateAsync<TResult>(string stateStore, string key)
        {
            var request = BuildMessage(MessageType.GetState, stateStore, key);
            var response = await SendMessageAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
                _logger.LogInformation("获取状态成功");
            else if (response.StatusCode == HttpStatusCode.NoContent)
                _logger.LogWarning("状态键不存在");
            else if (response.StatusCode == HttpStatusCode.BadRequest)
                _logger.LogError("状态存贮不存在或者配置错误");
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
                _logger.LogError("状态获取失败");

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
        /// Stores one state value in the globally configured Dapr state store.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to store.</typeparam>
        /// <param name="key">The state key under which the value is stored.</param>
        /// <param name="value">The value to serialize and store.</param>
        /// <exception cref="Exception">Thrown when no global state store name has been configured.</exception>
        public async Task SetStateAsync<TValue>(string key, TValue value)
        {
            if (_options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            await SetStateAsync<TValue>(_options.StateStoreName, key, value);
        }

        /// <summary>
        /// Stores one state value in a named Dapr state store.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to store.</typeparam>
        /// <param name="stateStore">The configured Dapr state store component name.</param>
        /// <param name="key">The state key under which the value is stored.</param>
        /// <param name="value">The value to serialize and store.</param>
        public async Task SetStateAsync<TValue>(string stateStore, string key, TValue value)
        {
            var state = new StateStore(key, value);
            await SetStatesAsync(stateStore, new List<StateStore> { state });
        }

        /// <summary>
        /// Stores multiple state values in the globally configured Dapr state store.
        /// </summary>
        /// <param name="values">The state entries to serialize and store as one Dapr state operation.</param>
        /// <exception cref="Exception">Thrown when no global state store name has been configured.</exception>
        public async Task SetStatesAsync(IEnumerable<StateStore> values)
        {
            if (_options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            await SetStatesAsync(_options.StateStoreName, values);
        }

        /// <summary>
        /// Stores multiple state values in a named Dapr state store.
        /// </summary>
        /// <param name="stateStore">The configured Dapr state store component name.</param>
        /// <param name="values">The state entries to serialize and store as one Dapr state operation.</param>
        public async Task SetStatesAsync(string stateStore, IEnumerable<StateStore> values)
        {
            var request = BuildMessage(MessageType.SetState, stateStore, "", values);
            var response = await SendMessageAsync(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
                _logger.LogInformation("状态保存成功");
            else if (response.StatusCode == HttpStatusCode.BadRequest)
                _logger.LogError("状态存储丢失或配置错误或请求格式错误");
            else if(response.StatusCode == HttpStatusCode.InternalServerError)
                _logger.LogError($"状态保存失败");
        }

        /// <summary>
        /// Deletes one state value from the globally configured Dapr state store.
        /// </summary>
        /// <param name="key">The state key to delete.</param>
        /// <exception cref="Exception">Thrown when no global state store name has been configured.</exception>
        public async Task DelStateAsync(string key)
        {
            if (_options.StateStoreName == null)
                throw new Exception("没有配置全局StateStoreName");
            await DelStateAsync(_options.StateStoreName, key);
        }

        /// <summary>
        /// Deletes one state value from a named Dapr state store.
        /// </summary>
        /// <param name="stateStore">The configured Dapr state store component name.</param>
        /// <param name="key">The state key to delete.</param>
        /// <exception cref="HttpRequestException">Thrown when the Dapr sidecar cannot be reached.</exception>
        public async Task DelStateAsync(string stateStore, string key)
        {
            var request = BuildMessage(MessageType.DeleteState, stateStore, key);
            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
                _logger.LogInformation("状态删除成功");
            else if (response.StatusCode == HttpStatusCode.BadRequest)
                _logger.LogError("状态存储丢失或配置错误");
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
                _logger.LogError($"状态删除失败");
        }

        /// <summary>
        /// Checks whether the local Dapr sidecar is ready to serve requests.
        /// The check retries until the configured connection retry count is reached and
        /// treats an HTTP 204 response from the sidecar health endpoint as healthy.
        /// </summary>
        /// <returns>
        /// A task that completes with <see langword="true"/> when the sidecar responds
        /// successfully within the retry window; otherwise, <see langword="false"/>.
        /// </returns>
        public async Task<bool> HealthCheckAsync()
        {
            int retryCount = 0;
            bool isHealth = false;
            if (!isHealth)
            {
                while (retryCount < _options.DaprConnectionRetryCount)
                {
                    try
                    {
                        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{_options.ApiPort}/v1.0/healthz"));
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
        /// Sends an already constructed request to the local Dapr sidecar.
        /// </summary>
        /// <param name="message">The request containing the Dapr route, HTTP method, and optional body.</param>
        /// <returns>A task that completes with the sidecar's HTTP response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the sidecar cannot be reached.</exception>
        private async Task<HttpResponseMessage> SendMessageAsync(HttpRequestMessage message)
        {
            return await _httpClient.SendAsync(message);
        }

        /// <summary>
        /// Builds a Dapr sidecar request for invocation, pub/sub, or state management.
        /// When a payload is supplied, it is serialized as UTF-8 JSON and assigned to the
        /// resulting request.
        /// </summary>
        /// <param name="requestType">The Dapr operation that determines the route and HTTP method.</param>
        /// <param name="host">The service, pub/sub component, or state store name used in the route.</param>
        /// <param name="path">The method name, topic name, or state key used in the route.</param>
        /// <param name="data">The optional payload to serialize as JSON.</param>
        /// <returns>A configured <see cref="HttpRequestMessage"/> targeting the local Dapr sidecar.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="requestType"/> is not supported.</exception>
        private HttpRequestMessage BuildMessage(MessageType requestType, string host, string path, object? data = null)
        {
            string bathUrl = $"http://localhost:{_options.ApiPort}";
            string version = "v1.0";
            string url;

            HttpRequestMessage request;
            // Map the logical operation to the corresponding Dapr sidecar route and HTTP verb.
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
            // Serialize payloads only for operations that provide a request body.
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
