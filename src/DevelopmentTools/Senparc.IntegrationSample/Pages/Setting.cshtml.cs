/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Setting.cshtml.cs
    文件功能描述：Setting.cshtml 相关实现
    
    
    创建标识：Senparc - 20240316
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;

namespace Senparc.IntegrationSample.Pages
{
    public class SettingModel : PageModel
    {
        public SenparcCoreSetting SenparcCoreSetting { get; set; }

        public NcfCoreState NcfCoreState { get; set; }

        public List<Assembly> XncfAssemblies { get; set; }

        public void OnGet()
        {
            SenparcCoreSetting = SiteConfig.SenparcCoreSetting with { };

            NcfCoreState = SiteConfig.NcfCoreState with { };

            XncfAssemblies = AssembleScanHelper.GetAssembiles();
        }
    }
}
