/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Skill.cshtml.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20261020
    
    Modified by: Senparc - 20261021
    Change description: v0.20.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.8 enhance Sandbox functionality with language validation and request handling improvements

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
