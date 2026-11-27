/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Senparc.Xncf.Accounts.Areas.Account.Pages related implementation

    Created by: Senparc - 20201226

    Modified by: Senparc - 20261127
    Change description: v0.3.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.Accounts.Areas.Account.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        /// <summary>
        /// Handles the account module administration page GET request.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
