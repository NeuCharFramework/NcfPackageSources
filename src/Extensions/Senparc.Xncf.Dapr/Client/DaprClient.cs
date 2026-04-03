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
        public DaprClient(HttpClient httpClient, ILogger<DaprClient> logger, ISerializer serializer, IOptions<DaprClientOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serializer = serializer;
            _options = options.Value;
        }

        /// <summary>
        /// service call
        /// </summary>
        /// <typeparam name="TResult">Returned data type</typeparam>
        /// <param name="invokeType">Request type</param>
        /// <param name="serviceId">Service name (App-Id)</param>
        /// <param name="methodName">method path</param>
        /// <param name="data">Request data</param>
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

            if (response.StatusCode == HttpStatusCode.NotFound)
                _logger.LogError("Unsupplied method name");
            else if (response.StatusCode == HttpStatusCode.Forbidden)
                _logger.LogError("Access control prohibits calls");
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
                _logger.LogError("Request failed");

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
        /// publish event
        /// </summary>
        /// <param name="topicName">Topic name</param>
        /// <param name="data">Post content</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task PublishEventAsync(string topicName, object data)
        {
            if(_options.PubSubName == null)
                throw new Exception("No global PubSubName configured");
            await PublishEventAsync(_options.PubSubName, topicName, data);
        }

        /// <summary>
        /// publish event
        /// </summary>
        /// <param name="pubSubName">Publish and subscribe component name</param>
        /// <param name="topicName">Topic name</param>
        /// <param name="data">publish data</param>
        /// <returns></returns>
        public async Task PublishEventAsync(string pubSubName, string topicName, object data)
        {
            var request = BuildMessage(MessageType.Publish, pubSubName, topicName, data);
            var response = await SendMessageAsync(request);
        }

        /// <summary>
        /// Get status
        /// </summary>
        /// <typeparam name="TResult">Return status type</typeparam>
        /// <param name="key">cache key</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<TResult> GetStateAsync<TResult>(string key)
        {
            if (_options.StateStoreName == null)
                throw new Exception("Global StateStoreName is not configured");
            return await GetStateAsync<TResult>(_options.StateStoreName, key);
        }

        /// <summary>
        /// Get status
        /// </summary>
        /// <typeparam name="TResult">Return status type</typeparam>
        /// <param name="stateStore">State storage component name</param>
        /// <param name="key">cache key</param>
        /// <returns></returns>
        public async Task<TResult> GetStateAsync<TResult>(string stateStore, string key)
        {
            var request = BuildMessage(MessageType.GetState, stateStore, key);
            var response = await SendMessageAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
                _logger.LogInformation("Get status successfully");
            else if (response.StatusCode == HttpStatusCode.NoContent)
                _logger.LogWarning("Status key does not exist");
            else if (response.StatusCode == HttpStatusCode.BadRequest)
                _logger.LogError("The state store does not exist or is misconfigured.");
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
                _logger.LogError("Status acquisition failed");

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
        /// save state
        /// </summary>
        /// <typeparam name="TValue">status type</typeparam>
        /// <param name="key">cache key</param>
        /// <param name="value">cache data</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SetStateAsync<TValue>(string key, TValue value)
        {
            if (_options.StateStoreName == null)
                throw new Exception("Global StateStoreName is not configured");
            await SetStateAsync<TValue>(_options.StateStoreName, key, value);
        }

        /// <summary>
        /// save state
        /// </summary>
        /// <typeparam name="TValue">status type</typeparam>
        /// <param name="stateStore">State storage component name</param>
        /// <param name="key">cache key</param>
        /// <param name="value">cache data</param>
        /// <returns></returns>
        public async Task SetStateAsync<TValue>(string stateStore, string key, TValue value)
        {
            var state = new StateStore(key, value);
            await SetStatesAsync(stateStore, new List<StateStore> { state });
        }

        /// <summary>
        /// Save multiple states
        /// </summary>
        /// <typeparam name="TValue">status type</typeparam>
        /// <param name="key">cache key</param>
        /// <param name="values">cache data</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SetStatesAsync(IEnumerable<StateStore> values)
        {
            if (_options.StateStoreName == null)
                throw new Exception("Global StateStoreName is not configured");
            await SetStatesAsync(_options.StateStoreName, values);
        }

        /// <summary>
        /// Save multiple states
        /// </summary>
        /// <typeparam name="TValue">status type</typeparam>
        /// <param name="stateStore">State storage component name</param>
        /// <param name="key">cache key</param>
        /// <param name="values">cache data</param>
        /// <returns></returns>
        public async Task SetStatesAsync(string stateStore, IEnumerable<StateStore> values)
        {
            var request = BuildMessage(MessageType.SetState, stateStore, "", values);
            var response = await SendMessageAsync(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
                _logger.LogInformation("Status saved successfully");
            else if (response.StatusCode == HttpStatusCode.BadRequest)
                _logger.LogError("The state store is missing or misconfigured or the request is malformed");
            else if(response.StatusCode == HttpStatusCode.InternalServerError)
                _logger.LogError($"State saving failed");
        }

        /// <summary>
        /// delete status
        /// </summary>
        /// <param name="key">cache key</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task DelStateAsync(string key)
        {
            if (_options.StateStoreName == null)
                throw new Exception("Global StateStoreName is not configured");
            await DelStateAsync(_options.StateStoreName, key);
        }

        /// <summary>
        /// delete status
        /// </summary>
        /// <param name="stateStore">State storage component name</param>
        /// <param name="key">cache key</param>
        /// <returns></returns>
        public async Task DelStateAsync(string stateStore, string key)
        {
            var request = BuildMessage(MessageType.DeleteState, stateStore, key);
            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
                _logger.LogInformation("Status deleted successfully");
            else if (response.StatusCode == HttpStatusCode.BadRequest)
                _logger.LogError("The state store is missing or misconfigured");
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
                _logger.LogError($"Status deletion failed");
        }

        /// <summary>
        /// Confirm whether Dapr Sidecar is running normally
        /// </summary>
        /// <returns></returns>
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
                            _logger.LogWarning($"DaprNot ready yet!");
                            await Task.Delay(100);
                        }                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"DaprNot ready yet,{e.Message}");
                        break;
                    }
                }
            }
            return isHealth;
        }

        /// <summary>
        /// Post messages using Http client
        /// </summary>
        /// <param name="message">Httprequest message</param>
        /// <returns>Httpreturn message</returns>
        private async Task<HttpResponseMessage> SendMessageAsync(HttpRequestMessage message)
        {
            return await _httpClient.SendAsync(message);
        }

        /// <summary>
        /// Build request message
        /// </summary>
        /// <param name="requestType">Dapr API type called</param>
        /// <param name="host">Service name (App-Id)</param>
        /// <param name="path">Resource path</param>
        /// <param name="data">Carrying data</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private HttpRequestMessage BuildMessage(MessageType requestType, string host, string path, object? data = null)
        {
            string bathUrl = $"http://localhost:{_options.ApiPort}";
            string version = "v1.0";
            string url;

            HttpRequestMessage request;
            //Build request URL
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
            //Build request body
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
