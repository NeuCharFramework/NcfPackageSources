using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.XncfBase;

namespace Senparc.IntegrationSample.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IList<IXncfRegister> XncfRegisterList { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            XncfRegisterList = Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList;
        }
    }
}
