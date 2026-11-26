/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.DynamicData.Areas.DynamicData.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        /// <summary>Initializes the DynamicData module index page.</summary>
        /// <param name="xncfModuleService">The lazy module service used by the base page model.</param>
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        /// <summary>Initializes the DynamicData index page request.</summary>
        public void OnGet()
        {
        }
    }
}
