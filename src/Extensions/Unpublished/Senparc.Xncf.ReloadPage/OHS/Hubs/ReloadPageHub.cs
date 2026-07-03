/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ReloadPageHub.cs
    文件功能描述：ReloadPageHub 相关实现
    
    
    创建标识：Senparc - 20211215
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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