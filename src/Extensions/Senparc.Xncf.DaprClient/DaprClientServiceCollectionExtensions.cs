using System;
using System.Linq;
using Senparc.Xncf.DaprClient;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for <see cref="IServiceCollection" />.
    /// </summary>
    public static class DaprClientServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Dapr client services to the provided <see cref="IServiceCollection" />. This does not include integration
        /// with ASP.NET Core MVC. Use the <c>AddDapr()</c> extension method on <c>IMvcBuilder</c> to register MVC integration.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" />.</param>
        /// <param name="configure"></param>
        public static void AddDaprClient(this IServiceCollection services)
        {
            services.AddHttpClient<DaprClient>();
        }
    }
}