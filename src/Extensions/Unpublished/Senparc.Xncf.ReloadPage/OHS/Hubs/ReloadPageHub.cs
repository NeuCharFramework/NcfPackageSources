/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ReloadPageHub.cs
    File description: ReloadPageHub related implementation

    Created by: Senparc - 20211215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.SignalR;

namespace Senparc.Xncf.ReloadPage.OHS.Hubs
{
    /// <summary>
    /// refreshpage
    /// </summary>
    public class ReloadPageHub : Hub
    {
        public const string Route = "/reloadHub";
    }
}