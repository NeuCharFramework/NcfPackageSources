/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseUpgrade.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.35.0 Updated module behavior and related support.

    Modified by: Senparc - 20260808
    Change description: v0.35.1 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Senparc.Web.Pages.Maintenance;

public sealed class DatabaseUpgradeModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Append("X-Robots-Tag", "noindex, nofollow");
    }
}
