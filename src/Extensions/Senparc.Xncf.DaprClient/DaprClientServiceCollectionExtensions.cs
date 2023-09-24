using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xncf.DaprClient
{
    public static class DaprClientServiceCollectionExtensions
    {
        public static void AddDaprClient(this IServiceCollection services)
        {
            services.AddHttpClient<DaprClient>();
        }
    }
}