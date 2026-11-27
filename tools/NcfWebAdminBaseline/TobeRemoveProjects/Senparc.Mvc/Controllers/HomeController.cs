/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HomeController.cs
    File description: Senparc.Mvc.Controllers related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.0.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Mvc.Filter;
using Microsoft.AspNetCore.Mvc;
using Senparc.Mvc.Models.VD;

namespace Senparc.Mvc.Controllers
{
    [MenuFilter("Home")]
    public class HomeController : BaseFrontController
    {
       
        /// <summary>
        /// Initializes the legacy front-end home controller.
        /// </summary>
        public HomeController()
        {
         
        }

        /// <summary>
        /// Renders the legacy home page with its default view model.
        /// </summary>
        /// <returns>The home view result.</returns>
        public ActionResult Index()
        {
            var vd = new Home_IndexVD()
            {
            };
         
            return View(vd);
        }

        /// <summary>
        /// Renders a legacy detail page for the supplied identifier.
        /// The current sample view model does not persist the identifier.
        /// </summary>
        /// <param name="id">The requested detail identifier.</param>
        /// <returns>The detail view result.</returns>
        public ActionResult Detail(int id)
        {
            var vd = new Home_DetailVD()
            {
            };
            return View(vd);
        }
        /// <summary>
        /// Renders the legacy list page.
        /// </summary>
        /// <returns>The list view result.</returns>
        public ActionResult List()
        {
            return View();
        }
    }
}
