using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;

namespace Senparc.IntegrationSample.Pages
{
    public class SettingModel : PageModel
    {
        public SenparcCoreSetting SenparcCoreSetting { get; set; }

        public NcfCoreState NcfCoreState { get; set; }

        public void OnGet()
        {
            SenparcCoreSetting = SiteConfig.SenparcCoreSetting with { };

            NcfCoreState = SiteConfig.NcfCoreState with { };
        }
    }
}
