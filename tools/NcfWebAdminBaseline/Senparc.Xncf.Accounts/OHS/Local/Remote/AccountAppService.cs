using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Accounts.OHS.Local.Remote
{
    public class AccountAppService: AppServiceBase
    {
        public AccountAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Performs a lightweight external HTTP request used by the Accounts
        /// keep-alive demonstration endpoint.
        /// </summary>
        /// <returns>A response containing the remote response body.</returns>
        [ApiBind]
        public async Task<AppResponseBase<string>> KeepAlive()
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var content = await Senparc.CO2NET.HttpUtility.RequestUtility.HttpGetAsync(base.ServiceProvider, "https://www.ncf.pub", Encoding.UTF8);
                return content;
            });
        }
    }
}
