using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Senparc.Web.Pages
{
    public class DocModel : PageModel
    {
        /// <summary>
        /// Redirects the legacy documentation page to the maintained documentation site.
        /// </summary>
        /// <returns>An HTTP redirect result.</returns>
        public IActionResult OnGet()
        {
            return Redirect("https://doc.ncf.pub");
        }
    }
}
