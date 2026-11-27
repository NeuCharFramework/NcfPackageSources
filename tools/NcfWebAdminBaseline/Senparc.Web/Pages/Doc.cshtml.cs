/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Doc.cshtml.cs
    File description: Senparc.Web.Pages related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.39.10 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
