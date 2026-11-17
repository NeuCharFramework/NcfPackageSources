using Senparc.Ncf.Core.Models.VD;
using Microsoft.AspNetCore.Mvc;
using Senparc.Core.Models.VD;

namespace Senparc.Mvc.Controllers
{
    public class ErrorController : BaseController
    {
        public ActionResult Error()
        {
            Error_ExceptionVD vd = new Error_ExceptionVD()
            {
                //COCONET temporarilyhide
                //Legacy comment translated to English.
            };
            return View(vd);
        }

        public ActionResult Error404(string aspxerrorpath)
        {
            Error_Error404VD vd = new Error_Error404VD()
            {
                Url = aspxerrorpath
            };
            return View(vd);
        }
    }
}