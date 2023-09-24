using System;
using System.Linq;
using Senparc.Xncf.DaprClient;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DaprClientServiceCollectionExtensions
    {
        public static void AddDaprClient(this IServiceCollection services)
        {
            services.AddHttpClient<DaprClient>();
        }
    }
}