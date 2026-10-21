/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Skill.cshtml.cs
    文件功能描述：PromptRange Skill 管理页
    
    
    创建标识：Senparc - 20261020
    
    修改标识：Senparc - 20261021
    修改描述：v0.20.0 提供 Skill 管理页

----------------------------------------------------------------*/

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
