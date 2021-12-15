using Microsoft.AspNetCore.SignalR;

namespace Senparc.Xncf.ReloadPage.OHS.Hubs
{
    /// <summary>
    /// 刷新页面
    /// </summary>
    public class ReloadPageHub : Hub
    {
        public const string Route = "/reloadHub";
    }
}