/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ReloadPageHub.cs
    File description: ReloadPageHub related implementation

    Created by: Senparc - 20211215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

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