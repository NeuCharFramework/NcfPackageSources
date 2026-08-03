/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseUpgrade.cshtml.cs
    文件功能描述：数据库升级维护页

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.35.0 新增数据库升级维护流程与多平台下载入口

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Web.Infrastructure.Database;

namespace Senparc.Web.Pages.Maintenance;

public sealed class DatabaseUpgradeModel : PageModel
{
    private readonly DatabaseRuntimeStateStore _stateStore;

    public DatabaseUpgradeModel(DatabaseRuntimeStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public DatabaseRuntimeState State { get; private set; }

    public void OnGet()
    {
        State = _stateStore.Current;
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    }
}
