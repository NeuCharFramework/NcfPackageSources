using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.PromptRange.Areas.PromptRange.Pages;

public class SkillModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    public SkillModel(Lazy<XncfModuleService> xncfModuleService)
        : base(xncfModuleService)
    {
    }

    public void OnGet()
    {
    }
}
