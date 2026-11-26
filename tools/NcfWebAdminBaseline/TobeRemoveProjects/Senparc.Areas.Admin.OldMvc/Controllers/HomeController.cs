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
