using System.Net;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Json;

namespace Senparc.Xncf.DaprClient
{
    public class DaprClient
    {
        private readonly HttpClient _httpClient;
        public DaprClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object> GetStateAsync<T>(string host, string key)
        {
            var request = BuildMessage(MessageType.GetState, host, key);
            return await _httpClient.SendAsync(request);
        }

        public async Task SetStateAsync<T>(string host, string key, T data, int ttl = -1)
        {
            var request = BuildMessage(MessageType.Invoke, host, key, data);
        }

        public async Task DelStateAsync(string key)
        {
            throw new NotImplementedException();
        }   

        public async Task<TResult> InvokeMethodAsync<TResult>(HttpMethod methodType ,string host, string path, object data, JsonSerializerOptions? options)
        {
            var request = BuildMessage(MessageType.Invoke, host, path, data, methodType);
            var response = await _httpClient.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TResult>(json, options);
            return result;
        }

        public async Task PublishEventAsync(string pubSubName, string topicName, object data)
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

        private async Task SendMessage(MessageType requestType, string host, string path, object? data = null, HttpMethod? methodType = null)
        {
            var request = BuildMessage(requestType, host, path, data, methodType);
            var response = await _httpClient.SendAsync(request);
        }

        private HttpRequestMessage BuildMessage(MessageType requestType, string host, string path, object? data = null, HttpMethod? methodType = null)
        {
            string bathUrl = "http://localhost:3500";
            string url;
            HttpRequestMessage request;
            switch (requestType)
            {
                case MessageType.Invoke:
                    url = $"{bathUrl}/v1.0/invoke/{host}/method/{path}";
                    if(methodType == null) throw new NotImplementedException();
                    request = new(methodType, url) { Version = new Version(1, 1) };
                    string json = JsonSerializer.Serialize(data);
                    request.Content = new StringContent(json);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue($"application/json");
                    break;
                case MessageType.Publish:
                    url = $"{bathUrl}/v1.0/publish/{host}/{path}";
                    request = new(HttpMethod.Post, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.SetState:
                    url = $"{bathUrl}/v1.0/state/{host}/{path}";
                    request = new(HttpMethod.Post, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.GetState:
                    url = $"{bathUrl}/v1.0/state/{host}/{path}";
                    request = new(HttpMethod.Get, url) { Version = new Version(1, 1) };
                    break;
                case MessageType.DeleteState:
                    url = $"{bathUrl}/v1.0/state/{host}/{path}";
                    request = new(HttpMethod.Delete, url) { Version = new Version(1, 1) };
                    break;
                default:
                    throw new ArgumentException();
            }
            return request;
        }
    }
}
