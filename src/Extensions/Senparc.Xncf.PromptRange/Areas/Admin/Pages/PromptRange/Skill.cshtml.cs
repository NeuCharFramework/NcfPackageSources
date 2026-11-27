/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Skill.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20261020

    Modified by: Senparc - 20261021
    Change description: v0.20.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.8 enhance Sandbox functionality with language validation and request handling improvements

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.PromptRange.Areas.PromptRange.Pages;

public class SkillModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    /// <summary>Initializes the PromptRange skill page.</summary>
    /// <param name="xncfModuleService">The lazy XNCF module service.</param>
    public SkillModel(Lazy<XncfModuleService> xncfModuleService)
        : base(xncfModuleService)
    {
    }

    /// <summary>Initializes the skill page request.</summary>
    public void OnGet()
    {
    }
}
