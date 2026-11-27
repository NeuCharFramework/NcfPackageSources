/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HomeController.cs
    File description: Senparc.Areas.Admin.Controllers related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.0.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Models.VD;
using Senparc.Mvc.Filter;

namespace Senparc.Areas.Admin.Controllers
{
    [MenuFilter("Home")]
    public class HomeController : BaseAdminController
    {
        /// <summary>Renders the legacy administrator home page.</summary>
        /// <returns>The home view.</returns>
        public ActionResult Index()
        {
            var vd = new Home_IndexVD()
            {

            };
            return View(vd);
        }
    }
}
