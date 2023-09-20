using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DaprClient
{
    public class DaprClient
    {
        private readonly HttpClient _httpClient;
        public DaprClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object> GetStateAsync<T>(string key)
        {
            throw new NotImplementedException();
        }

        public async Task SetStateAsync<T>(string key, T data)
        {
            throw new NotImplementedException();
        }

        public async Task DelStateAsync(string key)
        {
            throw new NotImplementedException();
        }

        public async Task InvokeMethodAsync(string key)
        {
            throw new NotImplementedException();
        }

        public async Task PublishEventAsync(string pubSubName, string topicName, object data)
        {
            throw new NotImplementedException();
        }

        public async Task CheckServiceHealth(string serviceName)
        {
            throw new NotImplementedException();
        }

        private async Task SendMessage(MessageType requestType, string host, string path, object data)
        {
            var request = BuildMessage(requestType, host, path, data);
            var response = await _httpClient.SendAsync(request);
        }

        private HttpRequestMessage BuildMessage(MessageType requestType, string host, string path, object data)
        {
            string bathUrl = "http://localhost:3500";
            string url;
            HttpRequestMessage request;
            switch (requestType)
            {
                case MessageType.Invoke:
                    url = $"{bathUrl}/v1.0/invoke/{host}/method/{path}";
                    request = new(HttpMethod.Post, url) { Version = new Version(1, 1) };
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
