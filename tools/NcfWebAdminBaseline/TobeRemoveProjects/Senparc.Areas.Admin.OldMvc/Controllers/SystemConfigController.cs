/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemConfigController.cs
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
    [MenuFilter("SystemConfig")]
    public class SystemConfigController : BaseAdminController
    {
        /// <summary>Renders the legacy system configuration page.</summary>
        /// <returns>The configuration view.</returns>
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        /// <summary>
        /// Accepts the legacy configuration form and returns to the configuration page.
        /// </summary>
        /// <param name="model">The posted configuration model.</param>
        /// <returns>A redirect to the configuration page.</returns>
        public IActionResult Index(SystemConfig_IndexVD model)
        {
            return RedirectToAction("Index");
        }
    }
}
