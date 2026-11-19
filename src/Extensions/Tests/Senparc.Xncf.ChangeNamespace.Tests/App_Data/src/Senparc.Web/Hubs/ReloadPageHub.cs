using Microsoft.AspNetCore.SignalR;

namespace Senparc.Web.Hubs
{
    /// <summary>
    /// refreshpage
    /// </summary>
    public class ReloadPageHub : Hub
    {
        public const string Route = "/reloadHub";
    }
}